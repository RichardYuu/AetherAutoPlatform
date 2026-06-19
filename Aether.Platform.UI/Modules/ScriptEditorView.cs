using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Scripting;

namespace Aether.Platform.UI.Modules
{
    public partial class ScriptEditorView : UserControl, IModuleView
    {
        public string ModuleName => "ScriptEditor";

        private Panel _leftPanel;
        private ListBox _scriptList;
        private Panel _editorPanel;
        private RichTextBox _codeEditor;
        private RichTextBox _outputConsole;
        private Button _btnRun;
        private Button _btnPause;
        private Button _btnStep;
        private Button _btnStop;
        private Label _lblStatus;
        private Label _lblBindStatus;
        private CheckBox _chkAutoRun;
        private Panel _lineNumPanel;
        private Timer _highlightTimer;
        private Panel _watchPanel;            // 变量监视面板
        private ListBox _watchList;           // 变量列表
        private Panel _findPanel;             // 查找替换栏
        private TextBox _findBox;
        private TextBox _replaceBox;
        private Button _btnFindNext;
        private Button _btnReplace;
        private Button _btnReplaceAll;
        private Label _lblFindCount;

        private readonly LuaScriptEngine _engine;
        private readonly GlobalsRegistry _globals;
        private readonly Dictionary<string, string> _presetScripts = new Dictionary<string, string>();

        /// <summary>用户设置的断点行号 (1-based)</summary>
        private readonly HashSet<int> _breakpoints = new HashSet<int>();
        /// <summary>当前高亮的执行行</summary>
        private int _currentHighlightLine = -1;
        /// <summary>上一次语法高亮后的文本（避免重复高亮）</summary>
        private string _lastHighlightText;

        // Lua 语法高亮正则（编译缓存）
        private static readonly Regex LuaCommentRegex = new Regex(@"--.*$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex LuaString1Regex = new Regex(@"""[^""\\]*(?:\\.[^""\\]*)*""", RegexOptions.Compiled);
        private static readonly Regex LuaString2Regex = new Regex(@"'[^'\\]*(?:\\.[^'\\]*)*'", RegexOptions.Compiled);
        private static readonly Regex LuaNumberRegex = new Regex(@"\b\d+\.?\d*\b", RegexOptions.Compiled);
        private static readonly Regex LuaKeywordRegex = new Regex(
            @"\b(?:and|break|do|else|elseif|end|false|for|function|goto|if|in|local|nil|not|or|repeat|return|then|true|until|while)\b",
            RegexOptions.Compiled);

        private static readonly Color PanelBg = Color.FromArgb(245, 246, 248);
        private static readonly Color EditorBg = Color.FromArgb(30, 30, 30);
        private static readonly Color EditorFg = Color.FromArgb(220, 220, 220);
        private static readonly Color LineNumBg = Color.FromArgb(40, 40, 45);
        private static readonly Color LineNumFg = Color.FromArgb(100, 105, 115);
        private static readonly Color BreakpointRed = Color.FromArgb(220, 50, 50);
        private static readonly Color BreakpointBg = Color.FromArgb(60, 20, 20);
        private static readonly Color CurrentLineBg = Color.FromArgb(70, 60, 20);  // 当前行高亮
        private static readonly Color ConsoleBg = Color.FromArgb(20, 20, 20);
        private static readonly Color ConsoleFg = Color.FromArgb(180, 220, 180);
        private static readonly Color AccentBlue = Color.FromArgb(0, 120, 215);
        private static readonly Color KwBlue = Color.FromArgb(86, 156, 214);
        private static readonly Color StrOrange = Color.FromArgb(214, 157, 133);
        private static readonly Color CmtGreen = Color.FromArgb(87, 166, 74);
        private static readonly Color NumCyan = Color.FromArgb(78, 201, 176);

        public ScriptEditorView()
        {
            _globals = new GlobalsRegistry();
            _engine = new LuaScriptEngine(_globals);

            TryInjectBindings();

            _engine.OnOutput += AppendOutput;
            _engine.OnError += AppendError;
            _engine.OnStopped += () =>
            {
                if (!IsDisposed)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _btnRun.Enabled = true;
                        _btnPause.Enabled = false;
                        _btnStep.Enabled = false;
                        _btnStop.Enabled = false;
                        _lblStatus.Text = "就绪";
                        _lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                        ClearLineHighlight();
                    }));
                }
            };
            _engine.OnPaused += () =>
            {
                if (!IsDisposed)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _btnPause.Text = "▶ 继续";
                        _btnStep.Enabled = true;
                        _lblStatus.Text = "已暂停";
                        _lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                        HighlightCurrentLine(_engine.CurrentLine);
                        UpdateWatchPanel();
                    }));
                }
            };
            _engine.OnLineChanged += line =>
            {
                if (!IsDisposed)
                    BeginInvoke((Action)(() => HighlightCurrentLine(line)));
            };

            _highlightTimer = new Timer { Interval = 300 };
            _highlightTimer.Tick += (s, e) =>
            {
                _highlightTimer.Stop();
                ApplySyntaxHighlighting();
            };

            InitPresets();
            BackColor = PanelBg;
            BuildUI();
        }

        private void TryInjectBindings()
        {
            var bindings = HardwareBindings.Current;
            if (bindings != null)
                _engine.SetHardwareBindings(bindings);
        }

        private void BuildUI()
        {
            // 左侧脚本列表
            _leftPanel = new Panel
            {
                Width = 160, Dock = DockStyle.Left,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblScripts = new Label
            {
                Text = "脚本列表",
                Dock = DockStyle.Top, Height = 32,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(235, 240, 245)
            };

            _scriptList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            foreach (var key in _presetScripts.Keys)
                _scriptList.Items.Add(key);
            _scriptList.SelectedIndex = 0;
            _scriptList.SelectedIndexChanged += OnScriptSelected;

            _leftPanel.Controls.Add(_scriptList);
            _leftPanel.Controls.Add(lblScripts);

            // 右侧编辑区
            _editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = EditorBg,
                Padding = new Padding(8, 8, 8, 8)
            };

            // 工具栏
            var toolbar = new Panel
            {
                Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(50, 50, 55)
            };

            _btnRun = new Button
            {
                Text = "▶ 运行",
                Location = new Point(8, 4), Size = new Size(72, 28),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnRun.FlatAppearance.BorderSize = 0;
            _btnRun.Click += (s, e) => RunScript();

            _btnPause = new Button
            {
                Text = "|| 暂停",
                Location = new Point(86, 4), Size = new Size(68, 28),
                Font = new Font("Microsoft YaHei", 8.5f),
                BackColor = Color.FromArgb(243, 156, 18), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnPause.FlatAppearance.BorderSize = 0;
            _btnPause.Click += (s, e) =>
            {
                if (_engine.IsPaused)
                {
                    _engine.Resume();
                    _btnPause.Text = "|| 暂停";
                    _btnStep.Enabled = false;
                    _lblStatus.Text = "运行中...";
                    _lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                    ClearLineHighlight();
                }
                else
                {
                    _engine.Pause();
                }
            };

            _btnStep = new Button
            {
                Text = "▷| 单步",
                Location = new Point(158, 4), Size = new Size(64, 28),
                Font = new Font("Microsoft YaHei", 8.5f),
                BackColor = Color.FromArgb(0, 150, 136), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnStep.FlatAppearance.BorderSize = 0;
            _btnStep.Click += (s, e) =>
            {
                _btnStep.Enabled = false;
                _engine.StepAsync();
            };

            _btnStop = new Button
            {
                Text = "■ 停止",
                Location = new Point(226, 4), Size = new Size(68, 28),
                Font = new Font("Microsoft YaHei", 8.5f),
                BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += (s, e) => StopScript();

            _lblStatus = new Label
            {
                Text = "就绪",
                Location = new Point(300, 8), Size = new Size(90, 20),
                ForeColor = Color.FromArgb(39, 174, 96),
                Font = new Font("Microsoft YaHei", 9f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblBindStatus = new Label
            {
                Location = new Point(392, 8), Size = new Size(20, 20),
                Font = new Font("Microsoft YaHei", 8f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            UpdateBindStatus();

            _chkAutoRun = new CheckBox
            {
                Text = "自动运行",
                Location = new Point(416, 6), Size = new Size(72, 22),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = Color.FromArgb(180, 185, 195),
                BackColor = Color.FromArgb(50, 50, 55)
            };
            _chkAutoRun.CheckedChanged += (s, e) =>
            {
                if (_chkAutoRun.Checked && _scriptList.SelectedItem != null)
                {
                    AppendOutput("--- 自动运行已开启 ---");
                    RunScript();
                }
            };

            var btns = new[] {
                ("新建", 494, Color.FromArgb(0, 120, 215), (Action)(() => NewScript())),
                ("保存", 548, Color.FromArgb(39, 174, 96), (Action)(() => SaveScript())),
                ("加载", 602, Color.FromArgb(243, 156, 18), (Action)(() => LoadScript())),
                ("删除", 656, Color.FromArgb(231, 76, 60), (Action)(() => DeleteScript()))
            };

            foreach (var (text, x, color, action) in btns)
            {
                var b = new Button
                {
                    Text = text, Location = new Point(x, 4), Size = new Size(52, 28),
                    Font = new Font("Microsoft YaHei", 8f),
                    BackColor = color, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => action();
                toolbar.Controls.Add(b);
            }

            var btnClear = new Button
            {
                Text = "清除",
                Location = new Point(716, 4), Size = new Size(50, 28),
                Font = new Font("Microsoft YaHei", 8.5f),
                BackColor = Color.FromArgb(80, 80, 85), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => _outputConsole.Clear();

            toolbar.Controls.Add(_btnRun);
            toolbar.Controls.Add(_btnPause);
            toolbar.Controls.Add(_btnStep);
            toolbar.Controls.Add(_btnStop);
            toolbar.Controls.Add(_lblStatus);
            toolbar.Controls.Add(_lblBindStatus);
            toolbar.Controls.Add(_chkAutoRun);
            toolbar.Controls.Add(btnClear);

            // 查找替换栏（默认隐藏）
            _findPanel = new Panel
            {
                Dock = DockStyle.Top, Height = 30,
                BackColor = Color.FromArgb(60, 60, 65),
                Visible = false
            };

            var lblFind = new Label
            {
                Text = "查找:", ForeColor = Color.FromArgb(180, 185, 195),
                Location = new Point(8, 6), Size = new Size(38, 18),
                Font = new Font("Microsoft YaHei", 8.5f)
            };

            _findBox = new TextBox
            {
                Location = new Point(48, 5), Size = new Size(140, 20),
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = EditorFg,
                BorderStyle = BorderStyle.FixedSingle
            };
            _findBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { FindNext(); ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { HideFindPanel(); ev.SuppressKeyPress = true; }
            };

            var lblReplace = new Label
            {
                Text = "替换:", ForeColor = Color.FromArgb(180, 185, 195),
                Location = new Point(196, 6), Size = new Size(38, 18),
                Font = new Font("Microsoft YaHei", 8.5f)
            };

            _replaceBox = new TextBox
            {
                Location = new Point(236, 5), Size = new Size(140, 20),
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = EditorFg,
                BorderStyle = BorderStyle.FixedSingle
            };
            _replaceBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { ReplaceOne(); ev.SuppressKeyPress = true; }
                if (ev.KeyCode == Keys.Escape) { HideFindPanel(); ev.SuppressKeyPress = true; }
            };

            _btnFindNext = new Button
            {
                Text = "▼", Location = new Point(388, 4), Size = new Size(28, 22),
                Font = new Font("Microsoft YaHei", 8f),
                BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.FromArgb(180, 185, 195),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnFindNext.FlatAppearance.BorderSize = 0;
            _btnFindNext.Click += (s, e) => FindNext();

            _btnReplace = new Button
            {
                Text = "→", Location = new Point(420, 4), Size = new Size(28, 22),
                Font = new Font("Microsoft YaHei", 8f),
                BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnReplace.FlatAppearance.BorderSize = 0;
            _btnReplace.Click += (s, e) => ReplaceOne();

            _btnReplaceAll = new Button
            {
                Text = "全部", Location = new Point(452, 4), Size = new Size(42, 22),
                Font = new Font("Microsoft YaHei", 8f),
                BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnReplaceAll.FlatAppearance.BorderSize = 0;
            _btnReplaceAll.Click += (s, e) => ReplaceAll();

            _lblFindCount = new Label
            {
                Location = new Point(500, 6), Size = new Size(60, 18),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = Color.FromArgb(130, 135, 145),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnFindClose = new Button
            {
                Text = "✕", Location = new Point(560, 4), Size = new Size(24, 22),
                Font = new Font("Microsoft YaHei", 8f),
                BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.FromArgb(180, 185, 195),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnFindClose.FlatAppearance.BorderSize = 0;
            btnFindClose.Click += (s, e) => HideFindPanel();

            _findPanel.Controls.Add(lblFind);
            _findPanel.Controls.Add(_findBox);
            _findPanel.Controls.Add(lblReplace);
            _findPanel.Controls.Add(_replaceBox);
            _findPanel.Controls.Add(_btnFindNext);
            _findPanel.Controls.Add(_btnReplace);
            _findPanel.Controls.Add(_btnReplaceAll);
            _findPanel.Controls.Add(_lblFindCount);
            _findPanel.Controls.Add(btnFindClose);

            // 代码编辑器
            _lineNumPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 44,
                BackColor = LineNumBg,
                Padding = new Padding(2, 4, 2, 4)
            };

            _codeEditor = new RichTextBox
            {
                Dock = DockStyle.Top, Height = 280,
                Font = new Font("Consolas", 11f),
                BackColor = EditorBg,
                ForeColor = EditorFg,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                AcceptsTab = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            _codeEditor.TextChanged += (s, e) =>
            {
                _highlightTimer.Stop();
                _highlightTimer.Start();
                UpdateLineNumbers();
            };
            _codeEditor.VScroll += (s, e) => UpdateLineNumbers();
            _codeEditor.SizeChanged += (s, e) => UpdateLineNumbers();
            _codeEditor.KeyDown += OnEditorKeyDown;
            _codeEditor.Text = "print('Hello')";

            // 变量监视面板（右侧，暂停时显示）
            _watchPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 170,
                BackColor = Color.FromArgb(35, 35, 40),
                Padding = new Padding(4, 4, 4, 4),
                Visible = false
            };

            var lblWatchTitle = new Label
            {
                Text = "▎局部变量",
                Dock = DockStyle.Top, Height = 22,
                ForeColor = Color.FromArgb(86, 156, 214),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(35, 35, 40)
            };

            _watchList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5f),
                BackColor = Color.FromArgb(35, 35, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20
            };
            _watchList.DrawItem += OnDrawWatchItem;

            _watchPanel.Controls.Add(_watchList);
            _watchPanel.Controls.Add(lblWatchTitle);

            // 分割线
            var splitter = new Panel
            {
                Dock = DockStyle.Top, Height = 4,
                BackColor = Color.FromArgb(70, 70, 75)
            };

            // 输出控制台
            _outputConsole = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = ConsoleBg,
                ForeColor = ConsoleFg,
                BorderStyle = BorderStyle.None,
                Multiline = true,
                ReadOnly = true,
                Text = "Lua 脚本引擎就绪。MoonSharp 2.0\r\n断点: 行号区点击设置/取消 | Ctrl+F 查找 | Ctrl+H 替换\r\n"
            };

            _editorPanel.Controls.Add(_outputConsole);
            _editorPanel.Controls.Add(splitter);
            _editorPanel.Controls.Add(_watchPanel);
            _editorPanel.Controls.Add(_codeEditor);
            _editorPanel.Controls.Add(_lineNumPanel);
            _editorPanel.Controls.Add(_findPanel);
            _editorPanel.Controls.Add(toolbar);

            Controls.Add(_editorPanel);
            Controls.Add(_leftPanel);

            LoadSelectedScript();
        }

        private void OnEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowFindPanel(replace: false);
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                ShowFindPanel(replace: true);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && _findPanel.Visible)
            {
                HideFindPanel();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                FindNext();
                e.SuppressKeyPress = true;
            }
        }

        private void OnScriptSelected(object sender, EventArgs e)
        {
            LoadSelectedScript();
        }

        private void LoadSelectedScript()
        {
            var selected = _scriptList.SelectedItem?.ToString();
            if (selected != null && _presetScripts.TryGetValue(selected, out var script))
            {
                _codeEditor.Text = script;
                _codeEditor.SelectionStart = 0;
                _codeEditor.SelectionLength = 0;
            }
        }

        private async void RunScript()
        {
            // 如果正在运行且暂停中 → 先停止再重启
            if (_engine.IsRunning)
            {
                _engine.Stop();
                // 等待引擎完全停止
                await Task.Delay(50);
            }

            try
            {
                _engine.ResetScript();
                TryInjectBindings();

                // 同步断点到引擎
                _engine.Breakpoints = new HashSet<int>(_breakpoints);

                // 只存储脚本，不立即执行（避免双重执行）
                _engine.LoadScript(_codeEditor.Text);

                _btnRun.Enabled = false;
                _btnPause.Enabled = true;
                _btnPause.Text = "|| 暂停";
                _btnStep.Enabled = false;
                _btnStop.Enabled = true;
                _lblStatus.Text = "运行中...";
                _lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                _watchPanel.Visible = false;
                ClearLineHighlight();

                AppendOutput("--- 脚本开始执行 ---");

                await _engine.RunAsync();
            }
            catch (Exception ex)
            {
                AppendError($"引擎错误: {ex.Message}");
                _btnRun.Enabled = true;
                _btnPause.Enabled = false;
                _btnStep.Enabled = false;
                _btnStop.Enabled = false;
                _lblStatus.Text = "错误";
                _lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                _watchPanel.Visible = false;
            }
        }

        private void StopScript()
        {
            _engine.Stop();
            ClearLineHighlight();
            _watchPanel.Visible = false;
        }

        private void AppendOutput(string text)
        {
            if (IsDisposed) return;
            if (_outputConsole.InvokeRequired)
            {
                _outputConsole.BeginInvoke((Action)(() => AppendOutput(text)));
                return;
            }
            _outputConsole.AppendText(text + "\r\n");
        }

        private void AppendError(string text)
        {
            if (IsDisposed) return;
            if (_outputConsole.InvokeRequired)
            {
                _outputConsole.BeginInvoke((Action)(() => AppendError(text)));
                return;
            }
            int start = _outputConsole.TextLength;
            _outputConsole.AppendText(text + "\r\n");
            _outputConsole.Select(start, text.Length + 2);
            _outputConsole.SelectionColor = Color.FromArgb(255, 130, 130);
            _outputConsole.DeselectAll();
        }

        public void OnActivated()
        {
            UpdateBindStatus();
            _codeEditor.Focus();
        }
        public void OnDeactivated()
        {
            if (_engine.IsRunning)
                _engine.Stop();
        }
        public void RefreshData() { }

        // ============================================================
        //  文件操作
        // ============================================================

        private void NewScript()
        {
            string name = "新脚本" + (_presetScripts.Count + 1);
            _presetScripts[name] = "-- 新脚本\r\nprint('hello')";
            _scriptList.Items.Add(name);
            _scriptList.SelectedIndex = _scriptList.Items.Count - 1;
            AppendOutput($"--- 已创建: {name} ---");
        }

        private void SaveScript()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "Lua 脚本|*.lua",
                FileName = (_scriptList.SelectedItem?.ToString() ?? "script") + ".lua",
                Title = "保存脚本"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(sfd.FileName, _codeEditor.Text, Encoding.UTF8);
                    AppendOutput($"--- 已保存: {Path.GetFileName(sfd.FileName)} ---");
                }
                catch (Exception ex)
                {
                    AppendError($"保存失败: {ex.Message}");
                }
            }
        }

        private void LoadScript()
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "Lua 脚本|*.lua|所有文件|*.*",
                Title = "加载脚本"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var code = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                    var name = Path.GetFileNameWithoutExtension(ofd.FileName);
                    _presetScripts[name] = code;

                    if (!_scriptList.Items.Contains(name))
                        _scriptList.Items.Add(name);
                    _scriptList.SelectedItem = name;
                    _codeEditor.Text = code;
                    AppendOutput($"--- 已加载: {Path.GetFileName(ofd.FileName)} ---");
                }
                catch (Exception ex)
                {
                    AppendError($"加载失败: {ex.Message}");
                }
            }
        }

        private void DeleteScript()
        {
            var selected = _scriptList.SelectedItem?.ToString();
            if (selected == null) return;
            if (_presetScripts.Count <= 1)
            {
                AppendError("至少保留一个脚本");
                return;
            }

            _presetScripts.Remove(selected);
            _scriptList.Items.Remove(selected);
            if (_scriptList.Items.Count > 0)
                _scriptList.SelectedIndex = 0;
            AppendOutput($"--- 已删除: {selected} ---");
        }

        private void UpdateBindStatus()
        {
            if (_lblBindStatus == null) return;
            var boot = AppBootstrap.Instance;
            bool hasBindings = HardwareBindings.Current != null || boot?.HardwareService != null;
            _lblBindStatus.Text = hasBindings ? "\u25CF" : "\u25CB";
            _lblBindStatus.ForeColor = hasBindings ? Color.FromArgb(39, 174, 96) : Color.FromArgb(180, 180, 180);
            _lblBindStatus.Parent?.Invalidate();
        }
    }
}