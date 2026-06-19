using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aether.Platform.Core.Models;

namespace Aether.Platform.UI.Controls
{
    public class SimulationPanel : UserControl
    {
        private ComboBox _cmbMode;
        private ComboBox _cmbSpeed;
        private Label _lblStatus;
        private FlowLayoutPanel _devicePanel;
        private Dictionary<string, CheckBox> _deviceChecks = new Dictionary<string, CheckBox>();
        private Dictionary<string, Label> _deviceStatusLabels = new Dictionary<string, Label>();
        private Panel _quickActionPanel;
        private Button _btnTriggerBarcode;
        private Button _btnTriggerAlarm;
        private Button _btnSimPressure;
        private Button _btnSimTemp;
        private Button _btnStart;
        private Button _btnStop;
        private Button _btnPause;
        private Button _btnReset;
        private bool _expanded;
        private bool _isRunning;
        private bool _isPaused;

        private static readonly Color PurpleBg = Color.FromArgb(248, 249, 250);
        private static readonly Color PurpleAccent = Color.FromArgb(155, 89, 182);
        private static readonly Color GreenStatus = Color.FromArgb(39, 174, 96);
        private static readonly Color RedStatus = Color.FromArgb(231, 76, 60);
        private static readonly Color GrayStatus = Color.FromArgb(149, 165, 166);

        public event Action<SimulationMode> ModeChanged;
        public event Action<string> DeviceSimulationChanged;
        public event Action<string> QuickActionTriggered;
        public event Action<int> SpeedChanged;
        public event Action StartClicked;
        public event Action StopClicked;
        public event Action PauseClicked;
        public event Action ResetClicked;

        public SimulationMode CurrentMode => (SimulationMode)_cmbMode.SelectedIndex;

        public SimulationPanel()
        {
            BackColor = PurpleBg;
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(0, 4, 0, 0);
            MinimumSize = new Size(200, 48);
            Height = 48;
            InitializeUI();
        }

        private void InitializeUI()
        {
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 4, 8, 4)
            };

            var expandBtn = new Label
            {
                Text = "▼",
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = PurpleAccent,
                Location = new Point(8, 12),
                Size = new Size(20, 20),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            expandBtn.Click += (s, e) => ToggleExpand();

            var titleLabel = new Label
            {
                Text = "仿真模式",
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                ForeColor = PurpleAccent,
                Location = new Point(32, 12),
                Size = new Size(80, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cmbMode = new ComboBox
            {
                Location = new Point(115, 11),
                Size = new Size(130, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9f)
            };
            _cmbMode.Items.AddRange(new[] { "全仿真模式", "部分仿真", "数据回放", "真实硬件" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += OnModeChanged;

            _lblStatus = new Label
            {
                Text = "已激活 — 无真实硬件",
                ForeColor = PurpleAccent,
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Italic),
                Location = new Point(255, 14),
                Size = new Size(170, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 速率选择
            _cmbSpeed = new ComboBox
            {
                Location = new Point(430, 11),
                Size = new Size(68, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei", 9f)
            };
            _cmbSpeed.Items.AddRange(new[] { "1x", "2x", "5x", "10x" });
            _cmbSpeed.SelectedIndex = 0;
            _cmbSpeed.SelectedIndexChanged += (s, e) =>
            {
                int speed = int.Parse(((string)_cmbSpeed.SelectedItem).TrimEnd('x'));
                SpeedChanged?.Invoke(speed);
            };

            // 运输按钮: ▶  ⏸  ⏹  ↺
            int btnX = 505;
            int btnW = 40;

            _btnStart = CreateTitleBtn("▶", btnX, btnW);
            _btnStart.Click += (s, e) =>
            {
                _isRunning = true; _isPaused = false;
                UpdateTransportBtns();
                StartClicked?.Invoke();
            };
            btnX += btnW + 4;

            _btnPause = CreateTitleBtn("⏸", btnX, btnW);
            _btnPause.Click += (s, e) =>
            {
                _isPaused = !_isPaused;
                UpdateTransportBtns();
                PauseClicked?.Invoke();
            };
            btnX += btnW + 4;

            _btnStop = CreateTitleBtn("⏹", btnX, btnW);
            _btnStop.Click += (s, e) =>
            {
                _isRunning = false; _isPaused = false;
                UpdateTransportBtns();
                StopClicked?.Invoke();
            };
            btnX += btnW + 4;

            _btnReset = CreateTitleBtn("↺", btnX, btnW);
            _btnReset.Click += (s, e) =>
            {
                _isRunning = false; _isPaused = false;
                UpdateTransportBtns();
                ResetClicked?.Invoke();
            };
            btnX += btnW + 4;

            _quickActionPanel = new Panel
            {
                Height = 36,
                BackColor = Color.Transparent,
                Visible = false,
                Padding = new Padding(8, 4, 8, 4)
            };

            _btnTriggerBarcode = CreateQuickBtn("扫码", 0, Color.FromArgb(41, 128, 185), (s, e) => QuickActionTriggered?.Invoke("Barcode"));
            _btnTriggerAlarm = CreateQuickBtn("报警", 72, Color.FromArgb(231, 76, 60), (s, e) => QuickActionTriggered?.Invoke("Alarm"));
            _btnSimPressure = CreateQuickBtn("压力", 144, Color.FromArgb(211, 84, 0), (s, e) => QuickActionTriggered?.Invoke("Pressure"));
            _btnSimTemp = CreateQuickBtn("温度", 216, Color.FromArgb(39, 174, 96), (s, e) => QuickActionTriggered?.Invoke("Temperature"));

            _quickActionPanel.Controls.Add(_btnTriggerBarcode);
            _quickActionPanel.Controls.Add(_btnTriggerAlarm);
            _quickActionPanel.Controls.Add(_btnSimPressure);
            _quickActionPanel.Controls.Add(_btnSimTemp);

            _devicePanel = new FlowLayoutPanel
            {
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(8, 4, 8, 4),
                Visible = false,
                WrapContents = true
            };

            BuildDeviceChecks();

            titleBar.Controls.Add(expandBtn);
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(_cmbMode);
            titleBar.Controls.Add(_lblStatus);
            titleBar.Controls.Add(_cmbSpeed);
            titleBar.Controls.Add(_btnStart);
            titleBar.Controls.Add(_btnPause);
            titleBar.Controls.Add(_btnStop);
            titleBar.Controls.Add(_btnReset);
            Controls.Add(titleBar);
            Controls.Add(_devicePanel);
            Controls.Add(_quickActionPanel);

            Layout += OnLayout;
        }

        private Button CreateTitleBtn(string text, int x, int w)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 11),
                Size = new Size(w, 24),
                Font = new Font("Microsoft YaHei", 10f),
                BackColor = Color.White,
                ForeColor = PurpleAccent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void UpdateTransportBtns()
        {
            _btnStart.Enabled = !_isRunning || _isPaused;
            _btnPause.Enabled = _isRunning;
            _btnStop.Enabled = _isRunning;
            _btnReset.Enabled = true;

            _btnStart.BackColor = _isRunning && !_isPaused ? Color.FromArgb(230, 230, 230) : Color.White;
            _btnPause.BackColor = _isPaused ? Color.FromArgb(255, 200, 100) : (_isRunning ? Color.White : Color.FromArgb(230, 230, 230));
            _btnStop.BackColor = Color.White;
        }

        private void BuildDeviceChecks()
        {
            var devices = new[]
            {
                new { Key = "PLC", Label = "PLC" },
                new { Key = "Camera", Label = "相机" },
                new { Key = "Scanner", Label = "扫码器" },
                new { Key = "Motion", Label = "运动轴" },
                new { Key = "Temperature", Label = "温控器" },
                new { Key = "Pressure", Label = "微压传感器" },
                new { Key = "EPValve", Label = "电气比例阀" },
                new { Key = "AnalogIO", Label = "模拟量IO" },
                new { Key = "SerialPort", Label = "串口" },
                new { Key = "Modbus", Label = "Modbus" },
                new { Key = "Scale", Label = "称重" },
                new { Key = "HeightGauge", Label = "测高仪" },
                new { Key = "ExposureMeter", Label = "曝光计" },
                new { Key = "SpotAnalyzer", Label = "光斑分析仪" },
            };

            int x = 8;
            foreach (var dev in devices)
            {
                var panel = new Panel
                {
                    Size = new Size(115, 20),
                    Margin = new Padding(0, 0, 4, 2),
                    BackColor = Color.Transparent
                };

                var cb = new CheckBox
                {
                    Text = dev.Label,
                    Checked = true,
                    Font = new Font("Microsoft YaHei", 8.5f),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(0, 1),
                    Size = new Size(75, 18),
                    Tag = dev.Key
                };
                cb.CheckedChanged += (s, e) =>
                {
                    var c = (CheckBox)s;
                    UpdateDeviceStatus(c.Tag?.ToString(), c.Checked);
                    DeviceSimulationChanged?.Invoke(c.Tag?.ToString());
                };

                var statusDot = new Label
                {
                    Text = "●",
                    Font = new Font("Microsoft YaHei", 8f),
                    ForeColor = GreenStatus,
                    Location = new Point(78, 1),
                    Size = new Size(16, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                panel.Controls.Add(cb);
                panel.Controls.Add(statusDot);

                _deviceChecks[dev.Key] = cb;
                _deviceStatusLabels[dev.Key] = statusDot;
                _devicePanel.Controls.Add(panel);
                x += 120;
            }
        }

        private Button CreateQuickBtn(string text, int x, Color color, EventHandler handler)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(8 + x, 3),
                Size = new Size(62, 28),
                Font = new Font("Microsoft YaHei", 8.5f),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += handler;
            return btn;
        }

        private void OnLayout(object sender, LayoutEventArgs e)
        {
            int y = 48;
            if (_expanded)
            {
                _devicePanel.Top = y;
                _devicePanel.Height = 46;
                _devicePanel.Visible = true;
                y += 46;
                _quickActionPanel.Top = y;
                _quickActionPanel.Visible = true;
                y += 40;
            }
            else
            {
                _devicePanel.Visible = false;
                _quickActionPanel.Visible = false;
            }
            Height = y;
        }

        private void ToggleExpand()
        {
            _expanded = !_expanded;

            var expandBtn = (Label)((Panel)Controls[0]).Controls[0];
            expandBtn.Text = _expanded ? "▲" : "▼";

            PerformLayout();
        }

        private void OnModeChanged(object sender, EventArgs e)
        {
            var mode = (SimulationMode)_cmbMode.SelectedIndex;
            ModeChanged?.Invoke(mode);

            switch (mode)
            {
                case SimulationMode.Full:
                    _lblStatus.Text = "已激活 — 全仿真";
                    _lblStatus.ForeColor = GreenStatus;
                    SetAllDevicesEnabled(true);
                    break;
                case SimulationMode.Partial:
                    _lblStatus.Text = "已激活 — 部分仿真";
                    _lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                    break;
                case SimulationMode.Replay:
                    _lblStatus.Text = "已激活 — 数据回放";
                    _lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                    SetAllDevicesEnabled(false);
                    break;
                case SimulationMode.None:
                    _lblStatus.Text = "真实硬件模式";
                    _lblStatus.ForeColor = RedStatus;
                    SetAllDevicesEnabled(false);
                    break;
            }
        }

        private void SetAllDevicesEnabled(bool enabled)
        {
            foreach (var cb in _deviceChecks.Values)
            {
                cb.Checked = enabled;
                cb.Enabled = enabled;
            }
        }

        public void UpdateDeviceStatus(string deviceKey, bool isConnected)
        {
            if (_deviceStatusLabels.TryGetValue(deviceKey, out var label))
            {
                label.ForeColor = isConnected ? GreenStatus : RedStatus;
                label.Text = isConnected ? "●" : "○";
            }
        }

        public void SetMode(SimulationMode mode)
        {
            _cmbMode.SelectedIndex = (int)mode;
        }

        public bool IsDeviceSimulated(string deviceKey)
        {
            return _deviceChecks.TryGetValue(deviceKey, out var cb) && cb.Checked;
        }
    }
}