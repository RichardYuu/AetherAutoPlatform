using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Models;

namespace Aether.Platform.UI.Shell
{
    public class MainShellForm : Form
    {
        private Panel _headerPanel;
        private Panel _sidebarPanel;
        private Panel _bottomTabBar;
        private Panel _contentPanel;
        private Controls.SimulationPanel _simPanel;

        private const int TopBarHeight = 64;
        private const int BottomTabHeight = 42;
        private const int SidebarWidth = 40;
        private Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>();
        private Dictionary<string, Button> _sideButtons = new Dictionary<string, Button>();
        private NavigationManager _navManager;
        private Label _lblPartNumber;
        private Label _lblPermissions;
        private Label _lblDateTime;
        private Label _lblDeviceId;
        private Timer _clockTimer;

        private static readonly Color HeaderBg = Color.FromArgb(240, 242, 245);
        private static readonly Color SidebarBg = Color.FromArgb(225, 232, 240);
        private static readonly Color TabBarBg = Color.FromArgb(225, 232, 240);
        private static readonly Color TabActiveBg = Color.FromArgb(0, 120, 215);
        private static readonly Color TabActiveText = Color.White;
        private static readonly Color TabInactiveBg = Color.FromArgb(210, 218, 226);
        private static readonly Color TabInactiveText = Color.FromArgb(60, 60, 60);
        private static readonly Color GreenBadge = Color.FromArgb(144, 238, 144);  // 浅绿，接近图片
        private static readonly Color SideBtnNormal = Color.FromArgb(200, 210, 220);
        private static readonly Color SideBtnRun = Color.FromArgb(76, 175, 80);
        private static readonly Color SideBtnPause = Color.FromArgb(255, 152, 0);
        private static readonly Color SideBtnStop = Color.FromArgb(244, 67, 54);
        private static readonly Color SideBtnReset = Color.FromArgb(33, 150, 243);

        public MainShellForm()
        {
            InitializeShell();
            _navManager = new NavigationManager(_contentPanel);

            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { if (!IsDisposed) _lblDateTime.Text = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"); };
            _clockTimer.Start();

            KeyPreview = true;
            KeyDown += OnKeyDown;
        }

        private void InitializeShell()
        {
            Text = "Aether\u81EA\u52A8\u5316\u5E73\u53F0";
            // 基于屏幕工作区（不含任务栏）计算初始大小
            var workArea = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(workArea.Width, 1600), (int)(workArea.Height * 0.88));
            MinimumSize = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(225, 232, 240);
            Font = new Font("Microsoft YaHei", 9f);
            KeyPreview = true;

            BuildHeader();
            BuildSimPanel();
            BuildBottomTabBar();
            BuildSidebar();
            BuildContentArea();

            Layout += PerformLayout2;
        }

        private void PerformLayout2(object sender, LayoutEventArgs e)
        {
            int topOffset = TopBarHeight;
            if (_simPanel != null && _simPanel.Visible)
                topOffset += _simPanel.Height;

            // 侧边栏：从 Header + SimPanel 下方开始，占左侧 SidebarWidth
            _sidebarPanel?.SetBounds(0, topOffset,
                SidebarWidth, ClientSize.Height - topOffset - BottomTabHeight);

            // 内容区：侧边栏右侧，填满剩余空间
            _contentPanel?.SetBounds(SidebarWidth, topOffset,
                ClientSize.Width - SidebarWidth, ClientSize.Height - topOffset - BottomTabHeight);
        }

        private void BuildHeader()
        {
            _headerPanel = new Panel
            {
                Height = 64,
                Dock = DockStyle.Top,
                BackColor = HeaderBg,
                Padding = new Padding(0)
            };

            // ---- 左：Logo + 公司名（紧贴最左侧）----
            var logoBox = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(4, 4),
                BackColor = Color.FromArgb(0, 80, 160),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            var companyNameCn = new Label
            {
                Text = "埃森科技有限公司",
                Location = new Point(48, 2),
                Size = new Size(320, 22),
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var companyNameEn = new Label
            {
                Text = "AETHER TECHNOLOGIES CO., LTD.",
                Location = new Point(48, 24),
                Size = new Size(320, 18),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ---- 中：设备名（大号居中，避免遮挡右侧控件）----
            var deviceTitle = new Label
            {
                Text = "泄露设备",
                Font = new Font("Microsoft YaHei", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 50, 100),
                Location = new Point(0, 8),
                Size = new Size(400, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ---- 右：件号 / 设备编号 / 权限 / 时间 ----
            _lblPartNumber = new Label
            {
                Text = "件号:AAAA",
                BackColor = GreenBadge,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                Size = new Size(105, 26),
                Location = new Point(0, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblDeviceId = new Label
            {
                Text = "SYCZ.ZDX211.0001-0001.01",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Size = new Size(230, 20),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblPermissions = new Label
            {
                Text = "无权限: -------",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(100, 100, 100),
                Size = new Size(120, 20),
                AutoSize = false,
                Location = new Point(0, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Size = new Size(230, 20),
                AutoSize = false,
                Location = new Point(0, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 统一处理 Header 适配：设备名居中 + 右侧控件锚定
            _headerPanel.Resize += (s, e) =>
            {
                deviceTitle.Left = (_headerPanel.Width - deviceTitle.Width) / 2;
                PositionRightControls();
            };

            _headerPanel.Controls.Add(logoBox);
            _headerPanel.Controls.Add(companyNameCn);
            _headerPanel.Controls.Add(companyNameEn);
            _headerPanel.Controls.Add(deviceTitle);
            _headerPanel.Controls.Add(_lblPartNumber);
            _headerPanel.Controls.Add(_lblDeviceId);
            _headerPanel.Controls.Add(_lblPermissions);
            _headerPanel.Controls.Add(_lblDateTime);

            Controls.Add(_headerPanel);
        }

        private void BuildSimPanel()
        {
            _simPanel = new Controls.SimulationPanel
            {
                Dock = DockStyle.Top
            };
            _simPanel.Resize += (s, e) => PerformLayout();
            _simPanel.ModeChanged += OnSimModeChanged;
            _simPanel.DeviceSimulationChanged += OnSimDeviceChanged;
            _simPanel.QuickActionTriggered += OnSimQuickAction;
            _simPanel.StartClicked += () => OnSimTransport("Start");
            _simPanel.StopClicked += () => OnSimTransport("Stop");
            _simPanel.PauseClicked += () => OnSimTransport("Pause");
            _simPanel.ResetClicked += () => OnSimTransport("Reset");
            _simPanel.SpeedChanged += (speed) => OnSimSpeedChanged(speed);
            Controls.Add(_simPanel);
        }

        private void OnSimModeChanged(SimulationMode mode)
        {
            var bootstrap = AppBootstrap.Instance;
            if (bootstrap == null) return;

            if (mode == SimulationMode.None)
            {
                bootstrap.Mode = RuntimeMode.Real;
            }
            else
            {
                bootstrap.Mode = RuntimeMode.Simulation;
            }

            bootstrap.OnSystemLog?.Invoke("SimPanel", $"仿真模式切换: {mode}");
        }

        private void OnSimDeviceChanged(string deviceKey)
        {
            var bootstrap = AppBootstrap.Instance;
            if (bootstrap == null) return;

            bool simulated = _simPanel.IsDeviceSimulated(deviceKey);
            bootstrap.OnSystemLog?.Invoke("SimPanel", $"设备 [{deviceKey}] 仿真: {(simulated ? "开启" : "关闭")}");
        }

        private void OnSimQuickAction(string actionType)
        {
            var bootstrap = AppBootstrap.Instance;
            if (bootstrap == null) return;

            switch (actionType)
            {
                case "Barcode":
                    System.Windows.Forms.MessageBox.Show($"模拟扫码数据: {DateTime.Now:yyyyMMddHHmmss}_OK", "仿真 — 扫码", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "Alarm":
                    System.Windows.Forms.MessageBox.Show($"模拟报警: 气压过低 @ {DateTime.Now:HH:mm:ss}", "仿真 — 报警", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "Pressure":
                    System.Windows.Forms.MessageBox.Show($"模拟压力值: 0.35 MPa (设定值 0.50±0.02)", "仿真 — 压力", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "Temperature":
                    System.Windows.Forms.MessageBox.Show($"模拟温度值: 23.5°C (±0.5°C)", "仿真 — 温度", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }

            bootstrap.OnSystemLog?.Invoke("SimPanel", $"快速操作触发: {actionType}");
        }

        private void OnSimTransport(string action)
        {
            var bootstrap = AppBootstrap.Instance;
            if (bootstrap == null) return;

            switch (action)
            {
                case "Start":
                    bootstrap.ActiveWorkflow?.Resume();
                    bootstrap.OnSystemLog?.Invoke("SimPanel", "仿真启动 ▶");
                    break;
                case "Stop":
                    bootstrap.ActiveWorkflow?.Abort();
                    bootstrap.OnSystemLog?.Invoke("SimPanel", "仿真停止 ⏹");
                    break;
                case "Pause":
                    bootstrap.ActiveWorkflow?.Pause();
                    bootstrap.OnSystemLog?.Invoke("SimPanel", "仿真暂停 ⏸");
                    break;
                case "Reset":
                    bootstrap.OnSystemLog?.Invoke("SimPanel", "仿真复位 ↺");
                    break;
            }
        }

        private void OnSimSpeedChanged(int speed)
        {
            var bootstrap = AppBootstrap.Instance;
            bootstrap?.OnSystemLog?.Invoke("SimPanel", $"仿真速率: {speed}x");
        }

        private void PositionRightControls()
        {
            if (_headerPanel == null || _headerPanel.Width < 100) return;

            int pad = 10; // 右侧留白
            // 右侧控件从右边缘锚定
            _lblDeviceId.Left = _headerPanel.Width - pad - _lblDeviceId.Width;
            _lblDateTime.Left = _lblDeviceId.Left;
            _lblPartNumber.Left = _lblDeviceId.Left - 6 - _lblPartNumber.Width;
            _lblPermissions.Left = _lblPartNumber.Left;
            // 设备名居中
            _lblDeviceId.Top = 6;
        }

        private void BuildSidebar()
        {
            _sidebarPanel = new Panel
            {
                Width = SidebarWidth,
                BackColor = SidebarBg,
                Padding = new Padding(2, 2, 2, 2)
            };

            // 竖排单字按钮，与图片一致
            var buttons = new[]
            {
                new { Name = "AutoMode", Text = "自\n动\n模\n式", Color = Color.FromArgb(70, 140, 210) },
                new { Name = "Run",       Text = "运\n行",     Color = SideBtnRun   },
                new { Name = "Pause",     Text = "暂\n停",     Color = SideBtnPause },
                new { Name = "Reset",     Text = "复\n位",     Color = SideBtnReset },
                new { Name = "Standby",   Text = "待\n机",     Color = Color.FromArgb(100, 160, 200) },
                new { Name = "EStop",     Text = "急\n停",     Color = SideBtnStop  },
            };

            int y = 4;
            foreach (var btnDef in buttons)
            {
                var btn = new Button
                {
                    Text = btnDef.Text,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(36, btnDef.Name == "AutoMode" ? 80 : 60),
                    Location = new Point(2, y),
                    BackColor = btnDef.Color,
                    ForeColor = Color.White,
                    Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = btnDef.Name
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.BorderColor = btnDef.Color;

                switch (btnDef.Name)
                {
                    case "EStop":
                        btn.Click += (s, e) =>
                        {
                            var r = MessageBox.Show("确认急停？", "急停", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (r == DialogResult.Yes) HighlightSideBtn("EStop");
                        };
                        break;
                    case "Run":    btn.Click += (s, e) => HighlightSideBtn("Run");    break;
                    case "Pause":  btn.Click += (s, e) => HighlightSideBtn("Pause");  break;
                    case "Reset":  btn.Click += (s, e) => HighlightSideBtn("Reset");  break;
                    case "Standby":btn.Click += (s, e) => HighlightSideBtn("Standby");break;
                }

                _sideButtons[btnDef.Name] = btn;
                _sidebarPanel.Controls.Add(btn);
                y += btn.Height + 4;
            }

            Controls.Add(_sidebarPanel);
        }

        private void BuildContentArea()
        {
            _contentPanel = new Panel
            {
                BackColor = Color.White
            };
            Controls.Add(_contentPanel);
        }

        private void BuildBottomTabBar()
        {
            _bottomTabBar = new Panel
            {
                Height = 42,
                Dock = DockStyle.Bottom,
                BackColor = TabBarBg,
                Padding = new Padding(0)
            };

            var tabs = new[] {
                "登录", "主界面", "状态日志", "控制调试",
                "视觉调试", "工艺调试", "系统参数", "历史记录", "版本信息",
                "仿真数据", "脚本编辑", "工作流"
            };
            var modules = new[] {
                "Login", "Main", "StatusLog", "ControlDebug",
                "VisionDebug", "ProcessDebug", "SystemConfig", "History", "VersionInfo",
                "SimDataEditor", "ScriptEditor", "WorkflowEditor"
            };

            int x = SidebarWidth; // 从侧边栏右侧开始
            for (int i = 0; i < tabs.Length; i++)
            {
                var btn = new Button
                {
                    Text = tabs[i],
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(90, 36),
                    Location = new Point(x, 3),
                    BackColor = (i == 1) ? TabActiveBg : TabInactiveBg,
                    ForeColor = (i == 1) ? TabActiveText : TabInactiveText,
                    Font = new Font("Microsoft YaHei", 10f),
                    Cursor = Cursors.Hand,
                    Tag = modules[i]
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.BorderColor = btn.BackColor;
                btn.Click += (s, e) => SwitchTab(((Button)s).Tag?.ToString());
                _tabButtons[modules[i]] = btn;
                _bottomTabBar.Controls.Add(btn);
                x += 92;
            }

            Controls.Add(_bottomTabBar);
        }

        public void SwitchTab(string moduleName)
        {
            _navManager?.NavigateTo(moduleName);
            foreach (var kv in _tabButtons)
            {
                bool active = kv.Key == moduleName;
                kv.Value.BackColor = active ? TabActiveBg : TabInactiveBg;
                kv.Value.ForeColor = active ? TabActiveText : TabInactiveText;
            }
        }

        private void HighlightSideBtn(string name)
        {
            foreach (var kv in _sideButtons)
            {
                kv.Value.FlatAppearance.BorderSize = (kv.Key == name) ? 3 : 0;
                kv.Value.FlatAppearance.BorderColor = Color.Yellow;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Keys.F1: SwitchTab("Main"); e.Handled = true; break;
                case Keys.F2: SwitchTab("StatusLog"); e.Handled = true; break;
                case Keys.F3: SwitchTab("ControlDebug"); e.Handled = true; break;
                case Keys.F4: SwitchTab("VisionDebug"); e.Handled = true; break;
                case Keys.F5: SwitchTab("ProcessDebug"); e.Handled = true; break;
                case Keys.F6: SwitchTab("SystemConfig"); e.Handled = true; break;
                case Keys.F7: SwitchTab("SimDataEditor"); e.Handled = true; break;
                case Keys.F8: SwitchTab("ScriptEditor"); e.Handled = true; break;
                case Keys.F9: SwitchTab("WorkflowEditor"); e.Handled = true; break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PositionRightControls();
            PerformLayout();
            _navManager.RegisterModule("Main", () => new Modules.MainView());
            _navManager.RegisterModule("StatusLog", () => new Modules.StatusLogView());
            _navManager.RegisterModule("ControlDebug", () => new Modules.ControlDebugView());
            _navManager.RegisterModule("VisionDebug", () => new Modules.VisionDebugView());
            _navManager.RegisterModule("ProcessDebug", () => new Modules.ProcessDebugView());
            _navManager.RegisterModule("SystemConfig", () => new Modules.SystemConfigView());
            _navManager.RegisterModule("History", () => new Modules.HistoryView());
            _navManager.RegisterModule("VersionInfo", () => new Modules.VersionInfoView());
            _navManager.RegisterModule("Login", () => new Modules.LoginView());
            _navManager.RegisterModule("SimDataEditor", () => new Modules.SimDataEditorView());
            _navManager.RegisterModule("ScriptEditor", () => new Modules.ScriptEditorView());
            _navManager.RegisterModule("WorkflowEditor", () => new Modules.WorkflowEditorView());
            SwitchTab("Main");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
