using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace Aether.Platform.Scripting
{
    /// <summary>
    /// Lua 脚本引擎，基于 MoonSharp 2.0
    /// 集成 MoonSharp 原生 IDebugger 实现精确行级断点和单步调试
    /// </summary>
    public class LuaScriptEngine
    {
        private Script _script;
        private ScriptDebugger _debugger;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly GlobalsRegistry _globals;
        private HardwareBindings _bindings;

        // 调试控制
        private volatile bool _breakRequested;
        private volatile bool _stepNext;

        public event Action<string> OnOutput;
        public event Action<string> OnError;
        public event Action OnStopped;
        public event Action OnPaused;
        /// <summary>当前执行行号变化事件 (0 表示无当前行)</summary>
        public event Action<int> OnLineChanged;

        public bool IsRunning => _isRunning;
        public bool IsPaused => _debugger != null && _debugger.IsAtBreak;
        public int CurrentLine => _debugger?.CurrentLine ?? 0;
        public string LastScript { get; private set; }

        /// <summary>当前断点行号集合 (1-based)</summary>
        public HashSet<int> Breakpoints { get; set; } = new HashSet<int>();

        /// <summary>暂停时可见的局部变量快照</summary>
        public IReadOnlyList<WatchItem> LocalWatches =>
            _debugger?.GetWatchesSnapshot() ?? new List<WatchItem>();

        public LuaScriptEngine(GlobalsRegistry globals = null)
        {
            _globals = globals ?? new GlobalsRegistry();
            ResetScript();
        }

        /// <summary>重置脚本环境</summary>
        public void ResetScript()
        {
            _script = new Script(CoreModules.Preset_Complete);
            _script.Options.DebugPrint = s => OnOutput?.Invoke(s);

            // 启用 MoonSharp 原生调试器
            _script.DebuggerEnabled = true;
            _debugger?.Detach();
            _debugger = new ScriptDebugger(this);
            _script.AttachDebugger(_debugger);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _isRunning = false;
            _breakRequested = false;
            _stepNext = false;

            InjectBuiltins();
            SyncGlobals();
        }

        /// <summary>绑定硬件操作函数</summary>
        public void SetHardwareBindings(HardwareBindings bindings)
        {
            _bindings = bindings;
            if (_bindings != null)
                _bindings.RegisterToScript(_script);
        }

        /// <summary>加载脚本文本（编译但不运行）</summary>
        public void LoadScript(string luaCode)
        {
            LastScript = luaCode;
        }

        /// <summary>异步运行脚本（支持断点和暂停）</summary>
        public async Task RunAsync()
        {
            if (_isRunning) return;
            if (string.IsNullOrEmpty(LastScript)) return;

            _isRunning = true;
            _breakRequested = false;
            _stepNext = false;
            _cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _script.DoString(LastScript);
                    }
                    catch (OperationCanceledException) { }
                    catch (ScriptRuntimeException ex)
                    {
                        // MoonSharp 内部通过 debugger 报错; 不在这里重复
                        if (!_cts.IsCancellationRequested)
                            OnError?.Invoke($"脚本运行时错误: {ex.DecoratedMessage}");
                    }
                    catch (Exception ex)
                    {
                        if (!_cts.IsCancellationRequested)
                            OnError?.Invoke($"执行错误: {ex.Message}");
                    }
                }, _cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                _breakRequested = false;
                _stepNext = false;
                _debugger?.ResetBreakState();
                OnStopped?.Invoke();
                SyncGlobals();
            }
        }

        /// <summary>暂停脚本执行</summary>
        public void Pause()
        {
            if (!_isRunning || (_debugger?.IsAtBreak ?? false)) return;
            _breakRequested = true;
        }

        /// <summary>恢复脚本执行</summary>
        public void Resume()
        {
            if (!_isRunning || !(_debugger?.IsAtBreak ?? false)) return;

            _breakRequested = false;
            _stepNext = false;
            _debugger.ResumeExecution();
        }

        /// <summary>单步执行 — 执行当前行后自动暂停</summary>
        public void StepAsync()
        {
            if (!_isRunning || !(_debugger?.IsAtBreak ?? false)) return;

            _breakRequested = false;
            _stepNext = true;
            _debugger.ResumeExecution();
        }

        /// <summary>停止脚本执行</summary>
        public void Stop()
        {
            _cts?.Cancel();
            _breakRequested = false;
            _stepNext = false;

            // 唤醒可能正阻塞的调试器
            _debugger?.ForceResume();
        }

        /// <summary>执行单个 Lua 表达式，返回结果</summary>
        public DynValue Evaluate(string luaExpression)
        {
            try
            {
                return _script.DoString(luaExpression);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"表达式错误: {ex.Message}");
                return DynValue.Nil;
            }
        }

        /// <summary>设置 Lua 全局变量</summary>
        public void SetGlobal(string name, object value)
        {
            _script.Globals[name] = DynValue.FromObject(_script, value);
        }

        /// <summary>获取 Lua 全局变量的值</summary>
        public DynValue GetGlobal(string name)
        {
            return _script.Globals.Get(name);
        }

        /// <summary>同步 GlobalsRegistry 到 Lua 全局变量</summary>
        private void SyncGlobals()
        {
            foreach (var kv in _globals.GetAll())
            {
                if (kv.Value is string s)
                    _script.Globals[kv.Key] = s;
                else if (kv.Value is double d)
                    _script.Globals[kv.Key] = d;
                else if (kv.Value is bool b)
                    _script.Globals[kv.Key] = b;
                else if (kv.Value is int i)
                    _script.Globals[kv.Key] = (double)i;
                else if (kv.Value is long l)
                    _script.Globals[kv.Key] = (double)l;
                else
                    _script.Globals[kv.Key] = kv.Value?.ToString();
            }
        }

        /// <summary>注入内建函数（print、sleep、log 等）</summary>
        private void InjectBuiltins()
        {
            _script.Globals["print"] = (Action<object>)(msg =>
                OnOutput?.Invoke(msg?.ToString() ?? "nil"));

            _script.Globals["msleep"] = (Action<int>)(ms => Thread.Sleep(ms));

            _script.Globals["log"] = (Action<string, string>)((level, msg) =>
                OnOutput?.Invoke($"[{level}] {msg}"));

            _script.Globals["assert"] = (Action<bool, string>)((cond, msg) =>
            {
                if (!cond) throw new ScriptRuntimeException(msg ?? "断言失败");
            });

            _script.Globals["now"] = (Func<string>)(() =>
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        // ============================================================
        //  MoonSharp IDebugger 实现 — 精确行级断点和单步
        // ============================================================

        private class ScriptDebugger : IDebugger
        {
            private readonly LuaScriptEngine _engine;
            private DebugService _debugService;
            private int _currentLine;
            private bool _isAtBreak;
            private readonly AutoResetEvent _resumeSignal = new AutoResetEvent(false);
            private readonly object _watchLock = new object();
            private List<WatchItem> _lastWatches = new List<WatchItem>();

            public int CurrentLine => _currentLine;
            public bool IsAtBreak => _isAtBreak;

            public ScriptDebugger(LuaScriptEngine engine)
            {
                _engine = engine;
            }

            public void Detach()
            {
                if (_debugService != null)
                {
                    _engine._script.DebuggerEnabled = false;
                    _debugService = null;
                }
            }

            public void ResetBreakState()
            {
                _isAtBreak = false;
                _currentLine = 0;
            }

            public void ResumeExecution()
            {
                _resumeSignal.Set();
            }

            public void ForceResume()
            {
                _isAtBreak = false;
                _resumeSignal.Set();
            }

            public IReadOnlyList<WatchItem> GetWatchesSnapshot()
            {
                lock (_watchLock)
                {
                    return new List<WatchItem>(_lastWatches);
                }
            }

            // ---- IDebugger 接口实现 ----

            public DebuggerCaps GetDebuggerCaps()
            {
                return DebuggerCaps.HasLineBasedBreakpoints;
            }

            public void SetDebugService(DebugService debugService)
            {
                _debugService = debugService;
            }

            public void SetSourceCode(SourceCode sourceCode) { }

            public void SetByteCode(string[] byteCode) { }

            public bool IsPauseRequested()
            {
                return _engine._breakRequested;
            }

            public bool SignalRuntimeException(ScriptRuntimeException ex)
            {
                // 运行异常时自动暂停以检查错误位置
                _engine._breakRequested = true;
                return true;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                // 更新当前行号
                int line = sourceref?.FromLine ?? 0;
                if (line > 0 && line != _currentLine)
                {
                    _currentLine = line;
                    // 通知引擎行号变化
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        _engine.OnLineChanged?.Invoke(line));
                }

                // 检查断点匹配
                bool isBreakpoint = _engine.Breakpoints.Contains(line);

                if (_engine._cts.IsCancellationRequested)
                {
                    _isAtBreak = false;
                    return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
                }

                // 断点或手动暂停请求
                if (isBreakpoint || _engine._breakRequested)
                {
                    _engine._breakRequested = false;

                    // 通知 UI 已暂停
                    _isAtBreak = true;
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        _engine.OnPaused?.Invoke();
                        _engine.OnLineChanged?.Invoke(line);
                    });

                    // 阻塞等待恢复指令
                    _resumeSignal.WaitOne();

                    _isAtBreak = false;

                    // 单步模式：执行完这一行后再次暂停
                    if (_engine._stepNext)
                    {
                        _engine._stepNext = false;
                        return new DebuggerAction { Action = DebuggerAction.ActionType.StepOver };
                    }

                    return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
                }

                return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
            }

            public void SignalExecutionEnded()
            {
                _isAtBreak = false;
                _currentLine = 0;
                _resumeSignal.Set();
            }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items)
            {
                // 保存监视变量快照（用于 UI 变量面板）
                if (watchType == WatchType.Locals)
                {
                    lock (_watchLock)
                    {
                        _lastWatches = new List<WatchItem>(items);
                    }
                }
            }

            public List<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
            {
                // 在源码中标记断点位置
                foreach (var sr in refs)
                {
                    if (!sr.CannotBreakpoint && _engine.Breakpoints.Contains(sr.FromLine))
                    {
                        sr.Breakpoint = true;
                    }
                }
            }
        }
    }
}
