using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;
using Aether.Platform.Devices;

namespace Aether.Platform.UI.Modules
{
    public class StatusLogView : UserControl, IModuleView
    {
        public string ModuleName => "StatusLog";
        private TextBox _logConsole;
        private Timer _refreshTimer;
        private List<StationCard> _stationCards = new List<StationCard>();
        private bool _useRealData = false;
        private string _lastSummary = "";

        private static readonly Color OkGreen = Color.FromArgb(76, 175, 80);
        private static readonly Color WarnOrange = Color.FromArgb(243, 156, 18);
        private static readonly Color ErrRed = Color.FromArgb(231, 76, 60);
        private static readonly Color IdleGray = Color.FromArgb(160, 165, 175);

        public StatusLogView()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Padding = new Padding(4);
            BuildLayout();
            StartRefresh();
        }

        private void BuildLayout()
        {
            // 上半部分：工位状态卡片网格
            var cardArea = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };

            for (int i = 1; i <= 8; i++)
            {
                var card = new StationCard(i);
                card.Location = new Point(4 + ((i - 1) % 4) * 234, 4 + ((i - 1) / 4) * 170);
                card.Click += (s, e) =>
                {
                    if (s is StationCard sc) AppendLog($"[工站{sc.StationNum}] 状态: {sc.CurrentStatus}");
                };
                _stationCards.Add(card);
                cardArea.Controls.Add(card);
            }
            Controls.Add(cardArea);

            // 下半部分：实时日志
            var logArea = new Panel { Dock = DockStyle.Bottom, Height = 150, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(20, 20, 25) };

            var logHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(45, 45, 55) };
            logHeader.Controls.Add(new Label { Text = "实时日志", Location = new Point(10, 4), Size = new Size(100, 20), ForeColor = Color.FromArgb(180, 200, 220), Font = new Font("Microsoft YaHei", 9f) });

            var btnPause = new Button { Text = "暂停", Location = new Point(logArea.Width - 130, 2), Size = new Size(55, 22), BackColor = Color.FromArgb(60, 60, 70), ForeColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Click += (s, e) => { btnPause.Text = btnPause.Text == "暂停" ? "恢复" : "暂停"; _refreshTimer.Enabled = btnPause.Text == "暂停"; };
            logHeader.Controls.Add(btnPause);

            var btnClear = new Button { Text = "清除", Location = new Point(logArea.Width - 70, 2), Size = new Size(55, 22), BackColor = Color.FromArgb(80, 80, 90), ForeColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => _logConsole.Clear();
            logHeader.Controls.Add(btnClear);

            logArea.Controls.Add(logHeader);

            _logConsole = new TextBox
            {
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 25), ForeColor = Color.FromArgb(200, 210, 200),
                Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
            };
            logArea.Controls.Add(_logConsole);
            Controls.Add(logArea);
        }

        private void StartRefresh()
        {
            _refreshTimer = new Timer { Interval = 2000 };
            _refreshTimer.Tick += (s, e) => RefreshAllStations();
            _refreshTimer.Start();
            RefreshAllStations();
        }

        private void RefreshAllStations()
        {
            var boot = AppBootstrap.Instance;
            string summary;

            if (boot != null && boot.IsInitialized)
            {
                _useRealData = true;
                UpdateFromRealData(boot);
                summary = $"机器: {boot.StateService?.MachineStatus}, 产量: {boot.ProductionSvc?.TotalCount ?? 0}";
            }
            else
            {
                if (_useRealData)
                {
                    AppendLog("[系统] 平台未初始化，切换至仿真模式");
                    _useRealData = false;
                }
                var rand = new Random();
                foreach (var card in _stationCards)
                    card.UpdateSimulated(rand);
                summary = "仿真模式";
            }

            // 仅在状态变化时记录日志，避免刷屏
            if (summary != _lastSummary)
            {
                _lastSummary = summary;
                AppendLog($"--- {summary} ({DateTime.Now:HH:mm:ss}) ---");
            }
        }

        private void UpdateFromRealData(AppBootstrap boot)
        {
            var state = boot.StateService;
            var prod = boot.ProductionSvc;
            var device = boot.ActiveDevice;

            var machineStatus = state?.MachineStatus ?? MachineStatus.Idle;
            var partNumber = state?.CurrentPartNumber ?? "---";
            int totalCount = prod?.TotalCount ?? 0;
            int okCount = prod?.OkCount ?? 0;
            int ngCount = prod?.NgCount ?? 0;
            int currentStep = device is Devices.Base.DeviceBase db ? db.CurrentStepIndex : -1;

            // 根据机器状态映射到工站卡片
            Color statusColor;
            string statusText;
            switch (machineStatus)
            {
                case MachineStatus.Running:
                    statusColor = OkGreen; statusText = "运行中"; break;
                case MachineStatus.Paused:
                    statusColor = WarnOrange; statusText = "暂停"; break;
                case MachineStatus.Error:
                case MachineStatus.Alarm:
                    statusColor = ErrRed; statusText = "报警"; break;
                case MachineStatus.Stopped:
                    statusColor = IdleGray; statusText = "停止"; break;
                default:
                    statusColor = IdleGray; statusText = "待机"; break;
            }

            // 根据设备工步数分散到卡片
            var steps = device is Devices.Base.DeviceBase devBase ? devBase.Steps : null;
            int totalSteps = steps?.Count ?? 0;
            int cardsPerStep = totalSteps > 0 ? Math.Max(1, 8 / Math.Max(1, totalSteps)) : 8;

            for (int i = 0; i < _stationCards.Count; i++)
            {
                var card = _stationCards[i];

                if (totalSteps > 0 && machineStatus == MachineStatus.Running)
                {
                    int stepForCard = i / cardsPerStep;
                    if (stepForCard == currentStep)
                    {
                        card.SetStatus("运行中", OkGreen);
                    }
                    else if (stepForCard < currentStep)
                    {
                        card.SetStatus("已完成", OkGreen);
                    }
                    else
                    {
                        card.SetStatus("等待中", IdleGray);
                    }
                }
                else
                {
                    card.SetStatus(statusText, statusColor);
                }

                card.SetProduct(partNumber != "---" ? partNumber : $"产品: P{i + 1001:D8}");
                card.SetCycleCount(i == 0 ? totalCount : totalCount > 0 ? totalCount - i : 0);

                // IO 位基于生产计数器
                var rng = new Random(okCount + ngCount + i * 7);
                card.UpdateIOBits(rng);
            }
        }

        private void AppendLog(string msg)
        {
            if (_logConsole != null && !this.IsDisposed && _logConsole.IsHandleCreated)
            {
                _logConsole.BeginInvoke((Action)(() =>
                {
                    _logConsole.AppendText(msg + "\r\n");
                    if (_logConsole.Lines.Length > 200)
                        _logConsole.Text = string.Join("\r\n", _logConsole.Lines.Skip(_logConsole.Lines.Length - 150));
                }));
            }
        }

        public void OnActivated() { _refreshTimer?.Start(); }
        public void OnDeactivated() { _refreshTimer?.Stop(); }
        public void RefreshData() { RefreshAllStations(); }
    }

    /// <summary>工位状态卡片控件</summary>
    public class StationCard : Panel
    {
        public int StationNum { get; }
        public string CurrentStatus { get; private set; } = "待机";
        private Label _statusLabel;
        private Label _productLabel;
        private Label _cycleCount;
        private Panel _ledIndicator;
        private Panel _ioBitsPanel;

        private static readonly Font BoldFont = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
        private static readonly Font SmallFont = new Font("Microsoft YaHei", 8f);
        private static readonly Color OkGreen = Color.FromArgb(76, 175, 80);
        private static readonly Color WarnOrange = Color.FromArgb(243, 156, 18);
        private static readonly Color ErrRed = Color.FromArgb(231, 76, 60);
        private static readonly Color IdleGray = Color.FromArgb(160, 165, 175);

        public StationCard(int stationNum)
        {
            StationNum = stationNum;
            Size = new Size(226, 158);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(6);

            BuildUI();
        }

        private void BuildUI()
        {
            // 工站编号 + LED
            var header = new Panel { Location = new Point(0, 0), Size = new Size(Width, 30), BackColor = Color.FromArgb(240, 243, 248) };
            _ledIndicator = new Panel { Location = new Point(6, 7), Size = new Size(14, 14), BackColor = IdleGray };
            header.Controls.Add(_ledIndicator);
            var title = new Label { Text = $"工站 {StationNum}", Location = new Point(26, 4), Size = new Size(120, 20), Font = BoldFont, ForeColor = Color.FromArgb(30, 30, 40) };
            header.Controls.Add(title);
            Controls.Add(header);

            // 状态
            _statusLabel = new Label { Text = "待机", Location = new Point(8, 36), Size = new Size(80, 24), Font = BoldFont, ForeColor = IdleGray, TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(_statusLabel);

            // IO 状态位
            var ioLabel = new Label { Text = "IO:", Location = new Point(90, 38), Size = new Size(28, 20), Font = SmallFont, ForeColor = Color.FromArgb(120, 120, 130) };
            Controls.Add(ioLabel);

            _ioBitsPanel = new Panel { Location = new Point(120, 38), Size = new Size(96, 20) };
            for (int i = 0; i < 8; i++)
            {
                var dot = new Panel { Location = new Point(i * 12, 3), Size = new Size(8, 8), Tag = i };
                _ioBitsPanel.Controls.Add(dot);
            }
            Controls.Add(_ioBitsPanel);

            // 产品
            _productLabel = new Label { Text = "产品: ---", Location = new Point(8, 68), Size = new Size(200, 20), Font = SmallFont, ForeColor = Color.FromArgb(100, 100, 110) };
            Controls.Add(_productLabel);

            // 周期计数
            _cycleCount = new Label { Text = "周期: 0", Location = new Point(8, 90), Size = new Size(100, 20), Font = SmallFont, ForeColor = Color.FromArgb(100, 100, 110) };
            Controls.Add(_cycleCount);

            // 时间
            Controls.Add(new Label { Text = DateTime.Now.ToString("HH:mm:ss"), Location = new Point(120, 90), Size = new Size(90, 20), Font = SmallFont, ForeColor = Color.FromArgb(150, 150, 150), TextAlign = ContentAlignment.MiddleRight });

            // 底部操作按钮
            var btnBar = new Panel { Location = new Point(0, 118), Size = new Size(Width, 34), BackColor = Color.FromArgb(248, 249, 252) };
            var btns = new[] { ("启动", Color.FromArgb(46, 125, 50)), ("暂停", Color.FromArgb(255, 160, 0)), ("复位", Color.FromArgb(211, 47, 47)) };
            int bx = 6;
            foreach (var (t, c) in btns)
            {
                var b = new Button { Text = t, Location = new Point(bx, 4), Size = new Size(60, 24), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = SmallFont };
                b.FlatAppearance.BorderSize = 0; btnBar.Controls.Add(b); bx += 66;
            }
            Controls.Add(btnBar);
        }

        public void SetStatus(string status, Color color)
        {
            CurrentStatus = status;
            _statusLabel.Text = status;
            _statusLabel.ForeColor = color;
            _ledIndicator.BackColor = color;
        }

        public void SetProduct(string productInfo)
        {
            if (!productInfo.StartsWith("产品:"))
                productInfo = "产品: " + productInfo;
            _productLabel.Text = productInfo;
        }

        public void SetCycleCount(int count)
        {
            _cycleCount.Text = $"周期: {count}";
        }

        public void UpdateSimulated(Random rng)
        {
            int r = rng.Next(10);

            switch (r)
            {
                case 0: case 1: case 2: case 3: case 4:  // 60% 运行中
                    SetStatus("运行中", OkGreen);
                    break;
                case 5: case 6:  // 20% 待机
                    SetStatus("待机", IdleGray);
                    break;
                case 7: case 8:  // 20% 暂停
                    SetStatus("暂停", WarnOrange);
                    break;
                case 9:           // 10% 报警
                    SetStatus("报警", ErrRed);
                    break;
            }

            _productLabel.Text = r <= 7 ? $"产品: P{rng.Next(1000, 9999):D8}" : "产品: ---";
            _cycleCount.Text = $"周期: {rng.Next(100, 9999)}";
            UpdateIOBits(rng);
        }

        public void UpdateIOBits(Random rng)
        {
            foreach (Control c in _ioBitsPanel.Controls)
            {
                if (c is Panel dot)
                {
                    dot.BackColor = rng.Next(2) == 0 ? OkGreen : IdleGray;
                }
            }
        }
    }
}