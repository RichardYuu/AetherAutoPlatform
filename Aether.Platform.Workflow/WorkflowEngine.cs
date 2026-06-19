using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Workflow
{
    /// <summary>工作流执行引擎</summary>
    public class WorkflowEngine
    {
        private readonly WorkflowDefinition _definition;
        private readonly WorkflowExecutionHooks _hooks;
        private readonly Dictionary<string, object> _variables;
        private CancellationTokenSource _cts;
        private WorkflowNode _currentNode;

        public WorkflowState State { get; private set; } = WorkflowState.Idle;
        public WorkflowNode CurrentNode => _currentNode;
        public WorkflowDefinition Definition => _definition;

        public event Action<WorkflowNode, NodeResult> OnNodeCompleted;
        public event Action<WorkflowNode> OnNodeStarted;
        public event Action<string> OnLog;
        public event Action<WorkflowState> OnStateChanged;

        public WorkflowEngine(WorkflowDefinition definition, WorkflowExecutionHooks hooks = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _hooks = hooks ?? new WorkflowExecutionHooks();
            _variables = new Dictionary<string, object>();
            _definition.RebuildConnections();
        }

        /// <summary>设置全局变量</summary>
        public void SetVariable(string key, object value) => _variables[key] = value;
        public T GetVariable<T>(string key, T defaultValue = default) =>
            _variables.TryGetValue(key, out var v) && v is T t ? t : defaultValue;

        /// <summary>启动工作流（异步执行）</summary>
        public async Task<WorkflowState> RunAsync(CancellationToken externalToken = default)
        {
            if (State == WorkflowState.Running) return State;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            SetState(WorkflowState.Running);

            try
            {
                var startNode = _definition.GetStartNode();
                if (startNode == null)
                {
                    Log("工作流缺少起始节点");
                    SetState(WorkflowState.Failed);
                    return State;
                }

                await ExecuteNodeAsync(startNode, _cts.Token);
                SetState(State == WorkflowState.Running ? WorkflowState.Completed : State);
            }
            catch (OperationCanceledException)
            {
                SetState(WorkflowState.Aborted);
            }
            catch (Exception ex)
            {
                Log($"工作流异常: {ex.Message}");
                SetState(WorkflowState.Failed);
            }

            return State;
        }

        /// <summary>暂停</summary>
        public void Pause() { if (State == WorkflowState.Running) SetState(WorkflowState.Paused); }

        /// <summary>恢复</summary>
        public void Resume() { if (State == WorkflowState.Paused) SetState(WorkflowState.Running); }

        /// <summary>中止</summary>
        public void Abort()
        {
            _cts?.Cancel();
            SetState(WorkflowState.Aborted);
        }

        /// <summary>重置所有节点状态</summary>
        public void Reset()
        {
            foreach (var node in _definition.Nodes)
            {
                node.Status = NodeResult.Skipped;
                node.ErrorMessage = null;
                node.StartedAt = null;
                node.CompletedAt = null;
            }
            _currentNode = null;
            _variables.Clear();
            SetState(WorkflowState.Idle);
        }

        /// <summary>获取已访问的节点 ID 列表（运行时追踪用）</summary>
        public List<string> GetExecutionPath()
        {
            return _definition.Nodes
                .Where(n => n.Status != NodeResult.Skipped)
                .OrderBy(n => n.StartedAt ?? DateTime.MinValue)
                .Select(n => n.Id)
                .ToList();
        }

        // ---- 核心执行 ----

        private async Task ExecuteNodeAsync(WorkflowNode node, CancellationToken ct)
        {
            _currentNode = node;
            node.Status = NodeResult.Running;
            node.StartedAt = DateTime.Now;
            OnNodeStarted?.Invoke(node);
            Log($"▶ 执行节点: {node.Name} [{node.Type}]");

            NodeResult result;
            try
            {
                result = await RunNodeInternal(node, ct);
            }
            catch (Exception ex)
            {
                result = NodeResult.Failure;
                node.ErrorMessage = ex.Message;
                Log($"✘ 节点失败: {node.Name} — {ex.Message}");
            }

            node.Status = result;
            node.CompletedAt = DateTime.Now;
            OnNodeCompleted?.Invoke(node, result);

            if (result == NodeResult.Failure)
            {
                SetState(WorkflowState.Failed);
                return;
            }

            ct.ThrowIfCancellationRequested();

            // 确定下一个节点
            var nextIds = _definition.GetOutgoingConnections(node.Id)
                .Select(c => c.ToNodeId)
                .ToList();

            // if no connections, use NextNodeIds as fallback
            if (nextIds.Count == 0)
                nextIds = node.NextNodeIds;

            foreach (var nextId in nextIds)
            {
                var nextNode = _definition.GetNode(nextId);
                if (nextNode == null) continue;
                if (nextNode.Type == WorkflowNodeType.End)
                {
                    _currentNode = nextNode;
                    nextNode.Status = NodeResult.Success;
                    nextNode.StartedAt = DateTime.Now;
                    nextNode.CompletedAt = DateTime.Now;
                    Log($"✓ 工作流结束");
                    return;
                }
                await ExecuteNodeAsync(nextNode, ct);
            }
        }

        private async Task<NodeResult> RunNodeInternal(WorkflowNode node, CancellationToken ct)
        {
            switch (node.Type)
            {
                case WorkflowNodeType.Start:
                case WorkflowNodeType.End:
                    return NodeResult.Success;

                case WorkflowNodeType.Delay:
                    int ms = node.GetProperty("Milliseconds", 1000);
                    await Task.Delay(ms, ct);
                    return NodeResult.Success;

                case WorkflowNodeType.Wait:
                    return await ExecuteWaitNode(node, ct);

                case WorkflowNodeType.SetVariable:
                    string vKey = node.GetProperty<string>("Key");
                    object vVal = node.GetProperty<object>("Value");
                    if (!string.IsNullOrEmpty(vKey)) _variables[vKey] = vVal;
                    return NodeResult.Success;

                case WorkflowNodeType.IfCondition:
                    return EvaluateCondition(node) ? NodeResult.Success : NodeResult.Skipped;

                case WorkflowNodeType.Loop:
                    return await ExecuteLoopNode(node, ct);

                case WorkflowNodeType.AxisMove:
                    return await ExecuteAxisMove(node, ct);

                case WorkflowNodeType.DioWrite:
                    return await ExecuteDioWrite(node, ct);

                case WorkflowNodeType.PlcWrite:
                    return await ExecutePlcWrite(node, ct);

                case WorkflowNodeType.VisionCapture:
                    return await ExecuteVisionCapture(node, ct);

                case WorkflowNodeType.ScannerRead:
                    return await ExecuteScannerRead(node, ct);

                case WorkflowNodeType.LuaScript:
                    return await ExecuteLua(node, ct);

                case WorkflowNodeType.Log:
                    Log(node.GetProperty("Message", ""));
                    return NodeResult.Success;

                default:
                    return NodeResult.Success;
            }
        }

        private async Task<NodeResult> ExecuteWaitNode(WorkflowNode node, CancellationToken ct)
        {
            int timeoutMs = node.GetProperty("TimeoutMs", 30000);
            string waitKey = node.GetProperty<string>("WaitVariable");
            object expectedVal = node.GetProperty<object>("ExpectedValue");

            int elapsed = 0;
            int pollMs = node.GetProperty("PollIntervalMs", 100);
            while (elapsed < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                if (State == WorkflowState.Paused)
                {
                    await Task.Delay(pollMs, ct);
                    continue;
                }
                if (!string.IsNullOrEmpty(waitKey) &&
                    _variables.TryGetValue(waitKey, out var current) &&
                    Equals(current, expectedVal))
                    return NodeResult.Success;
                await Task.Delay(pollMs, ct);
                elapsed += pollMs;
            }
            return NodeResult.Failure;
        }

        private async Task<NodeResult> ExecuteLoopNode(WorkflowNode node, CancellationToken ct)
        {
            int maxIterations = node.GetProperty("MaxIterations", 100);
            string loopChildId = node.GetProperty<string>("LoopBodyNodeId");
            var loopBody = _definition.GetNode(loopChildId);
            if (loopBody == null) return NodeResult.Failure;

            for (int i = 0; i < maxIterations; i++)
            {
                ct.ThrowIfCancellationRequested();
                _variables["LoopIndex"] = i;
                await ExecuteNodeAsync(loopBody, ct);
                if (State == WorkflowState.Failed || State == WorkflowState.Aborted)
                    return NodeResult.Failure;
            }
            return NodeResult.Success;
        }

        // ---- 硬件操作节点 ----

        private async Task<NodeResult> ExecuteAxisMove(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.AxisMoveAsync == null) return NodeResult.Success;  // 无硬件绑定则跳过
            string axisId = node.GetProperty<string>("AxisId");
            double targetPos = node.GetProperty("TargetPosition", 0.0);
            double speed = node.GetProperty("Speed", 50.0);
            bool ok = await _hooks.AxisMoveAsync(axisId, targetPos, speed, ct);
            return ok ? NodeResult.Success : NodeResult.Failure;
        }

        private async Task<NodeResult> ExecuteDioWrite(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.DioWriteAsync == null) return NodeResult.Success;
            int ioIndex = node.GetProperty("IoIndex", 0);
            bool state = node.GetProperty("State", false);
            bool ok = await _hooks.DioWriteAsync(ioIndex, state, ct);
            return ok ? NodeResult.Success : NodeResult.Failure;
        }

        private async Task<NodeResult> ExecutePlcWrite(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.PlcWriteAsync == null) return NodeResult.Success;
            string addr = node.GetProperty<string>("Address");
            object value = node.GetProperty<object>("Value");
            bool ok = await _hooks.PlcWriteAsync(addr, value, ct);
            return ok ? NodeResult.Success : NodeResult.Failure;
        }

        private async Task<NodeResult> ExecuteVisionCapture(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.VisionCaptureAsync == null) return NodeResult.Success;
            string modelName = node.GetProperty<string>("ModelName");
            string result = await _hooks.VisionCaptureAsync(modelName, ct);
            if (!string.IsNullOrEmpty(result))
                _variables["VisionResult"] = result;
            return string.IsNullOrEmpty(result) ? NodeResult.Failure : NodeResult.Success;
        }

        private async Task<NodeResult> ExecuteScannerRead(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.ScannerReadAsync == null) return NodeResult.Success;
            string barcode = await _hooks.ScannerReadAsync(ct);
            if (!string.IsNullOrEmpty(barcode))
                _variables["Barcode"] = barcode;
            return string.IsNullOrEmpty(barcode) ? NodeResult.Failure : NodeResult.Success;
        }

        private async Task<NodeResult> ExecuteLua(WorkflowNode node, CancellationToken ct)
        {
            if (_hooks.LuaExecuteAsync == null) return NodeResult.Success;
            string script = node.GetProperty<string>("Script");
            string result = await _hooks.LuaExecuteAsync(script, ct);
            return string.IsNullOrEmpty(result) ? NodeResult.Failure : NodeResult.Success;
        }

        // ---- 辅助 ----

        private bool EvaluateCondition(WorkflowNode node)
        {
            string expr = node.GetProperty<string>("Expression");
            string varKey = node.GetProperty<string>("VariableKey");
            string op = node.GetProperty<string>("Operator", "==");
            string compareVal = node.GetProperty<string>("CompareValue");

            if (string.IsNullOrEmpty(varKey) || !_variables.TryGetValue(varKey, out var actual))
                return false;

            switch (op)
            {
                case "==": return actual?.ToString() == compareVal;
                case "!=": return actual?.ToString() != compareVal;
                case ">":  return double.TryParse(actual?.ToString(), out var a) && double.TryParse(compareVal, out var b) && a > b;
                case "<":  return double.TryParse(actual?.ToString(), out var c) && double.TryParse(compareVal, out var d) && c < d;
                case ">=": return double.TryParse(actual?.ToString(), out var e) && double.TryParse(compareVal, out var f) && e >= f;
                case "<=": return double.TryParse(actual?.ToString(), out var g) && double.TryParse(compareVal, out var h) && g <= h;
                case "true": return actual is bool bv && bv;
                case "false": return !(actual is bool bv2 && bv2);
                default: return false;
            }
        }

        private void SetState(WorkflowState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void Log(string msg)
        {
            _hooks.LogCallback?.Invoke("Workflow", msg);
            OnLog?.Invoke(msg);
        }
    }
}
