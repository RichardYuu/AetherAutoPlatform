using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.UI.Modules
{
    public class MainView : UserControl, IModuleView
    {
        public string ModuleName => "Main";

        private Timer _refreshTimer;
        private readonly Random _rng = new Random();

        // 动态控件引用
        private readonly List<Label> _statValueLabels = new List<Label>();
        private DataGridView _productionDgv;
        private int _productionCounter;

        public MainView()
        {
            BackColor = Color.FromArgb(235, 240, 245);
            Padding = new Padding(6);
            BuildLayout();

            // 启动定时刷新
            _refreshTimer = new Timer { Interval = 2000 };
            _refreshTimer.Tick += (s, e) => OnRefreshTick();
            _refreshTimer.Start();
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            table.Controls.Add(BuildLeftColumn(), 0, 0);
            table.Controls.Add(BuildCenterColumn(), 1, 0);
            table.Controls.Add(BuildRightColumn(), 2, 0);

            Controls.Add(table);
        }

        private Panel BuildLeftColumn()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

            layout.Controls.Add(BuildProductionStatsPanel(), 0, 0);
            layout.Controls.Add(BuildProductionTablePanel(), 0, 1);
            layout.Controls.Add(BuildStationStatusPanel(), 0, 2);
            layout.Controls.Add(BuildIOPanel(), 0, 3);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel BuildCenterColumn()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));

            layout.Controls.Add(BuildMaterialCheckPanel(), 0, 0);
            layout.Controls.Add(BuildTrayGridPanel(), 0, 1);
            layout.Controls.Add(BuildCurvePanel(), 0, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel BuildRightColumn()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(4) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            layout.Controls.Add(BuildCameraMainPanel(), 0, 0);
            layout.Controls.Add(BuildCameraThumbPanel(), 0, 1);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel BuildProductionStatsPanel()
        {
            var pnl = CreateCard("生产统计");
            var grid = new TableLayoutPanel { Location = new Point(8, 28), Size = new Size(360, 200), ColumnCount = 2, RowCount = 6, BackColor = Color.Transparent, Padding = new Padding(4) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var items = new[] {
                ("总数", "10000", Color.FromArgb(50, 50, 50)),
                ("OK数", "9800", Color.FromArgb(76, 175, 80)),
                ("NG数", "100", Color.FromArgb(244, 67, 54)),
                ("抛料数", "100", Color.FromArgb(255, 152, 0)),
                ("良率", "95%", Color.FromArgb(76, 175, 80)),
                ("总时间", "105h", Color.FromArgb(50, 50, 50)),
                ("运行时间", "80h", Color.FromArgb(33, 150, 243)),
                ("效率", "28.8s", Color.FromArgb(50, 50, 50)),
                ("报警时间", "10h", Color.FromArgb(244, 67, 54)),
                ("待机时间", "10h", Color.FromArgb(150, 150, 150)),
                ("调试时间", "5h", Color.FromArgb(100, 100, 100)),
            };

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var row = new Panel { BackColor = Color.Transparent, Margin = new Padding(2) };
                row.Controls.Add(new Label { Text = item.Item1, Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.FromArgb(100, 100, 100), Location = new Point(2, 0), Size = new Size(80, 20) });
                var valLbl = new Label { Text = item.Item2, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = item.Item3, Location = new Point(2, 18), Size = new Size(80, 20) };
                row.Controls.Add(valLbl);
                _statValueLabels.Add(valLbl);
                grid.Controls.Add(row, i % 2, i / 2);
            }

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6f));

            pnl.Controls.Add(grid);
            return pnl;
        }

        private Panel BuildProductionTablePanel()
        {
            var pnl = CreateCard("生产信息");

            var dgv = new DataGridView
            {
                Location = new Point(8, 26),
                Size = new Size(pnl.Width - 20, pnl.Height - 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                Font = new Font("Microsoft YaHei", 8f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgv.Columns.Add("Time", "时间");
            dgv.Columns.Add("Code", "产品码");
            dgv.Columns.Add("Result", "结果");
            dgv.Columns.Add("Data", "数据");
            _productionDgv = dgv;

            var rows = new[] {
                new[] { "2024/05/24 08:32", "SN20240524001", "OK", "0.85" },
                new[] { "2024/05/24 08:33", "SN20240524002", "OK", "0.82" },
                new[] { "2024/05/24 08:33", "SN20240524003", "NG", "1.20" },
                new[] { "2024/05/24 08:34", "SN20240524004", "OK", "0.80" },
                new[] { "2024/05/24 08:34", "SN20240524005", "OK", "0.83" },
            };
            foreach (var r in rows) dgv.Rows.Add(r);

            pnl.Controls.Add(dgv);
            return pnl;
        }

        private Panel BuildStationStatusPanel()
        {
            var pnl = CreateCard("工站状态");
            var stations = new[] { ("工站1", true), ("工站2", true), ("工站3", true), ("工站4", true), ("工站5", false), ("工站6", true) };
            int x = 8;
            foreach (var (name, ok) in stations)
            {
                var card = new Panel
                {
                    Size = new Size(110, 75),
                    Location = new Point(x, 28),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = ok ? Color.FromArgb(230, 255, 230) : Color.FromArgb(255, 230, 230)
                };
                card.Controls.Add(new Label
                {
                    Text = name,
                    Location = new Point(4, 4), Size = new Size(100, 20),
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50)
                });
                card.Controls.Add(new Label
                {
                    Text = ok ? "正常" : "异常",
                    Location = new Point(4, 26), Size = new Size(100, 20),
                    Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                    ForeColor = ok ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54)
                });
                card.Controls.Add(new Label
                {
                    Text = ok ? "●运行中" : "●停止",
                    Location = new Point(4, 48), Size = new Size(100, 18),
                    Font = new Font("Microsoft YaHei", 8f),
                    ForeColor = Color.FromArgb(120, 120, 120)
                });
                pnl.Controls.Add(card);
                x += 118;
            }
            return pnl;
        }

        private Panel BuildIOPanel()
        {
            var pnl = CreateCard("相关IO");
            var items = new[] {
                ("安全门1", true), ("安全门2", true), ("安全门3", false),
                ("急停按钮", false), ("光栅", true), ("气压", true)
            };
            int y = 26;
            foreach (var (name, status) in items)
            {
                var dot = new Panel
                {
                    Size = new Size(10, 10),
                    Location = new Point(12, y + 4),
                    BackColor = status ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54)
                };
                dot.Paint += (s, e) => e.Graphics.FillEllipse(new SolidBrush(dot.BackColor), 0, 0, 10, 10);
                pnl.Controls.Add(dot);
                pnl.Controls.Add(new Label
                {
                    Text = $"{name}: {(status ? "正常" : "故障")}",
                    Location = new Point(28, y), Size = new Size(120, 18),
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = Color.FromArgb(60, 60, 60)
                });
                y += 22;
            }
            return pnl;
        }

        private Panel BuildMaterialCheckPanel()
        {
            var pnl = CreateCard("辅料信息 / 点检");
            var grid = new TableLayoutPanel { Location = new Point(6, 28), Size = new Size(pnl.Width - 16, 60), ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            var matInfo = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };
            matInfo.Controls.Add(new Label { Text = "胶型型号: ABC123def", Location = new Point(4, 4), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50) });
            matInfo.Controls.Add(new Label { Text = "胶水有效期: 2024/05/30 18:00", Location = new Point(4, 24), Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.FromArgb(244, 67, 54) });
            matInfo.Controls.Add(new Label { Text = "载具数: 20040530ABCdef", Location = new Point(4, 42), Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.FromArgb(100, 100, 100) });
            grid.Controls.Add(matInfo, 0, 0);

            var checkPanel = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(255, 240, 240), Dock = DockStyle.Fill };
            checkPanel.Controls.Add(new Label { Text = "点检状态", Location = new Point(4, 6), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50) });
            checkPanel.Controls.Add(new Label { Text = "未点检", Location = new Point(4, 28), Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(244, 67, 54) });
            grid.Controls.Add(checkPanel, 1, 0);

            pnl.Controls.Add(grid);
            return pnl;
        }

        private Panel BuildTrayGridPanel()
        {
            var pnl = CreateCard("料仓 / 料盘");
            var tabPanel = new Panel { Location = new Point(6, 28), Size = new Size(pnl.Width - 16, 28), BackColor = Color.Transparent };
            var tabs = new[] { "盘1", "盘2", "盘3" };
            for (int i = 0; i < 3; i++)
            {
                var tb = new Label
                {
                    Text = tabs[i], Location = new Point(i * 52, 2), Size = new Size(48, 22),
                    BackColor = i == 0 ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 210, 220),
                    ForeColor = i == 0 ? Color.White : Color.FromArgb(60, 60, 60),
                    TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 9f), Cursor = Cursors.Hand
                };
                tabPanel.Controls.Add(tb);
            }
            pnl.Controls.Add(tabPanel);

            var trayGrid = new TableLayoutPanel
            {
                Location = new Point(6, 56), Size = new Size(pnl.Width - 16, pnl.Height - 66),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 6, RowCount = 4, BackColor = Color.Transparent
            };

            var states = new[] { "空", "待确认", "待确认", "作业中", "作业完成", "作业完成",
                                 "空", "待确认", "待确认", "待作业", "作业中", "确认",
                                 "空", "空", "待确认", "待作业", "作业中", "作业完成",
                                 "空", "空", "空", "待确认", "待作业", "作业完成" };
            var colors = new Dictionary<string, Color> {
                {"空", Color.FromArgb(220, 220, 220)}, {"待确认", Color.FromArgb(255, 235, 180)},
                {"确认", Color.FromArgb(180, 230, 255)}, {"待作业", Color.FromArgb(255, 200, 140)},
                {"作业中", Color.FromArgb(33, 150, 243)}, {"作业完成", Color.FromArgb(76, 175, 80)}
            };

            for (int c = 0; c < 6; c++) trayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
            for (int r = 0; r < 4; r++) trayGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            for (int i = 0; i < Math.Min(states.Length, 24); i++)
            {
                int col = i % 6, row = i / 6;
                var cell = new Panel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = colors.TryGetValue(states[i], out var c) ? c : Color.Gray,
                    Margin = new Padding(2)
                };
                cell.Controls.Add(new Label
                {
                    Text = $"{row + 1}-{col + 1}\n{states[i]}",
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 7f, FontStyle.Bold),
                    ForeColor = (states[i] == "作业中" || states[i] == "作业完成") ? Color.White : Color.FromArgb(50, 50, 50)
                });
                trayGrid.Controls.Add(cell, col, row);
            }

            pnl.Controls.Add(trayGrid);

            var confirmBtn = new Button
            {
                Text = "料仓确认",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                Size = new Size(80, 26),
                Location = new Point(pnl.Width - 180, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            confirmBtn.FlatAppearance.BorderSize = 0;
            pnl.Controls.Add(confirmBtn);

            var cbLeft = new RadioButton { Text = "左料仓", Location = new Point(pnl.Width - 100, 28), Checked = true, Font = new Font("Microsoft YaHei", 8f), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var cbRight = new RadioButton { Text = "右料仓", Location = new Point(pnl.Width - 100, 48), Font = new Font("Microsoft YaHei", 8f), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            pnl.Controls.Add(cbLeft);
            pnl.Controls.Add(cbRight);

            return pnl;
        }

        private Panel BuildCurvePanel()
        {
            var pnl = CreateCard("曲线图");
            var curveGrid = new TableLayoutPanel
            {
                Location = new Point(6, 28), Size = new Size(pnl.Width - 16, pnl.Height - 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent
            };
            curveGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            curveGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            curveGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            curveGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            for (int i = 0; i < 4; i++)
            {
                var chart = new Panel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.FromArgb(248, 248, 248),
                    Margin = new Padding(2)
                };
                chart.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var r = ((Panel)s).ClientRectangle;
                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                    {
                        var pts = new PointF[8];
                        for (int j = 0; j < 8; j++)
                            pts[j] = new PointF(10 + j * (r.Width - 20) / 7f, r.Height - 20 - (float)(Math.Sin(j * 0.8 + i) * 12 + 15));
                        g.DrawLines(pen, pts);
                    }
                };
                var lbl = new Label { Text = $"曲线{i + 1}", Location = new Point(2, 2), Font = new Font("Microsoft YaHei", 7f), ForeColor = Color.FromArgb(120, 120, 120), AutoSize = true };
                chart.Controls.Add(lbl);
                curveGrid.Controls.Add(chart, i % 2, i / 2);
            }

            pnl.Controls.Add(curveGrid);
            return pnl;
        }

        private Panel BuildCameraMainPanel()
        {
            var pnl = CreateCard("拍照项目1");
            var camView = new Panel
            {
                Location = new Point(8, 26),
                Size = new Size(pnl.Width - 20, pnl.Height - 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(20, 22, 28),
                BorderStyle = BorderStyle.FixedSingle
            };
            camView.Controls.Add(new Label
            {
                Text = "相机预览区",
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(60, 65, 70), Font = new Font("Microsoft YaHei", 12f)
            });
            pnl.Controls.Add(camView);
            return pnl;
        }

        private Panel BuildCameraThumbPanel()
        {
            var pnl = CreateCard("拍照项目");
            var thumbGrid = new TableLayoutPanel
            {
                Location = new Point(8, 26),
                Size = new Size(pnl.Width - 20, pnl.Height - 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 3, RowCount = 3, BackColor = Color.Transparent
            };

            for (int i = 0; i < 3; i++) thumbGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            for (int i = 0; i < 3; i++) thumbGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            for (int i = 0; i < 9; i++)
            {
                var thumb = new Panel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = i == 0 ? Color.FromArgb(76, 175, 80) : Color.FromArgb(30, 32, 38),
                    Margin = new Padding(2)
                };
                thumb.Controls.Add(new Label
                {
                    Text = $"拍照\n项目{i + 1}",
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = i == 0 ? Color.White : Color.FromArgb(100, 105, 110),
                    Font = new Font("Microsoft YaHei", 7f)
                });
                thumbGrid.Controls.Add(thumb, i % 3, i / 3);
            }

            pnl.Controls.Add(thumbGrid);
            return pnl;
        }

        private Panel CreateCard(string title)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 140),
                Location = new Point(8, 6),
                Size = new Size(300, 20)
            });
            return card;
        }

        // ===== 动态数据刷新 =====

        private void OnRefreshTick()
        {
            var boot = AppBootstrap.Instance;
            if (boot != null && boot.IsInitialized)
            {
                UpdateFromRealData(boot);
            }
            else
            {
                UpdateFromSimulatedData();
            }
        }

        private void UpdateFromRealData(AppBootstrap boot)
        {
            var prod = boot.ProductionSvc;
            var state = boot.StateService;
            var device = boot.ActiveDevice;

            int total = prod?.TotalCount ?? 0;
            int ok = prod?.OkCount ?? 0;
            int ng = prod?.NgCount ?? 0;
            double yield = prod?.YieldRate ?? 100;
            double oee = prod?.OEE ?? 0;
            var machineStatus = state?.MachineStatus ?? MachineStatus.Idle;
            string partNumber = state?.CurrentPartNumber ?? "---";

            // 更新统计标签
            if (_statValueLabels.Count >= 8)
            {
                _statValueLabels[0].Text = total.ToString();
                _statValueLabels[1].Text = ok.ToString();
                _statValueLabels[2].Text = ng.ToString();
                _statValueLabels[3].Text = (total - ok - ng).ToString();   // 抛料 = 总数 - OK - NG
                _statValueLabels[4].Text = $"{yield:F1}%";
                _statValueLabels[5].Text = $"{total / 100:F0}h";
                _statValueLabels[6].Text = $"{oee * 100:F0}%";
                _statValueLabels[7].Text = machineStatus == MachineStatus.Running ? "运行" : machineStatus == MachineStatus.Idle ? "待机" : machineStatus == MachineStatus.Paused ? "暂停" : machineStatus == MachineStatus.Error ? "故障" : "停止";
            }

            // 添加生产数据行
            if (_productionDgv != null && _productionDgv.Rows.Count < 200 && total > _productionCounter)
            {
                var now = DateTime.Now;
                string code = $"SN{now:yyyyMMdd}{total:D5}";
                string result = ng > _lastNgCount ? "NG" : "OK";
                _productionDgv.Rows.Insert(0, now.ToString("yyyy/MM/dd HH:mm:ss"), code, result, yield.ToString("F2"));
                _lastNgCount = ng;
            }
            _productionCounter = total;

            // 更新工站状态
            if (device != null)
                UpdateStationStatusReal(machineStatus);

            // 更新 IO — 使用硬件服务
            UpdateIOStatusReal(boot.HardwareService);

            // 点检状态
            UpdateMaterialCheckReal(state);
        }

        private int _lastNgCount;

        private void UpdateStationStatusReal(MachineStatus status)
        {
            bool isRunning = status == MachineStatus.Running;
            bool isError = status == MachineStatus.Error || status == MachineStatus.Alarm;

            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var leftCol = table.GetControlFromPosition(0, 0) as Panel;
                    if (leftCol == null) continue;
                    foreach (Control cc in leftCol.Controls)
                    {
                        if (cc is TableLayoutPanel tlp)
                        {
                            var stationCard = tlp.GetControlFromPosition(0, 2) as Panel;
                            if (stationCard != null)
                            {
                                foreach (Control station in stationCard.Controls)
                                {
                                    if (station is Panel card && card.Controls.Count >= 3)
                                    {
                                        card.BackColor = isError ? Color.FromArgb(255, 230, 230)
                                            : isRunning ? Color.FromArgb(230, 255, 230)
                                            : Color.FromArgb(255, 248, 220);
                                        if (card.Controls[1] is Label statusLbl)
                                        {
                                            statusLbl.Text = isError ? "异常" : isRunning ? "正常" : "待机";
                                            statusLbl.ForeColor = isError ? Color.FromArgb(244, 67, 54)
                                                : isRunning ? Color.FromArgb(76, 175, 80)
                                                : Color.FromArgb(243, 156, 18);
                                        }
                                        if (card.Controls[2] is Label runLbl)
                                        {
                                            runLbl.Text = isError ? "●故障" : isRunning ? "●运行中" : "●待机";
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateIOStatusReal(IHardwareService hw)
        {
            if (hw == null) return;

            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var leftCol = table.GetControlFromPosition(0, 0) as Panel;
                    if (leftCol == null) continue;
                    foreach (Control cc in leftCol.Controls)
                    {
                        if (cc is TableLayoutPanel tlp)
                        {
                            var ioCard = tlp.GetControlFromPosition(0, 3) as Panel;
                            if (ioCard != null)
                            {
                                int itemIdx = 0;
                                foreach (Control io in ioCard.Controls)
                                {
                                    if (io is Panel dot && dot.Size.Width == 10)
                                    {
                                        bool ok;
                                        try
                                        {
                                            var dio = hw.GetDigitalIO(itemIdx.ToString());
                                            ok = dio?.ReadAsync(System.Threading.CancellationToken.None).Result ?? true;
                                        }
                                        catch { ok = _rng.NextDouble() > 0.05; }

                                        dot.BackColor = ok ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                                        if (itemIdx < ioCard.Controls.Count - 1 && ioCard.Controls[ioCard.Controls.IndexOf(dot) + 1] is Label ioLabel)
                                        {
                                            string baseName = ioLabel.Text.Split(':')[0];
                                            ioLabel.Text = $"{baseName}: {(ok ? "正常" : "故障")}";
                                        }
                                        itemIdx++;
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateMaterialCheckReal(IStateService state)
        {
            bool checked_ = state != null && state.MachineStatus != MachineStatus.Idle;

            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var centerCol = table.GetControlFromPosition(1, 0) as Panel;
                    if (centerCol == null) continue;
                    foreach (Control cc in centerCol.Controls)
                    {
                        if (cc is TableLayoutPanel ctlp)
                        {
                            var matCard = ctlp.GetControlFromPosition(0, 0) as Panel;
                            if (matCard != null)
                            {
                                foreach (Control mat in matCard.Controls)
                                {
                                    if (mat is TableLayoutPanel grid)
                                    {
                                        var checkCell = grid.GetControlFromPosition(1, 0) as Panel;
                                        if (checkCell != null)
                                        {
                                            checkCell.BackColor = checked_ ? Color.FromArgb(240, 255, 240) : Color.FromArgb(255, 240, 240);
                                            if (checkCell.Controls.Count >= 2 && checkCell.Controls[1] is Label checkLbl)
                                            {
                                                checkLbl.Text = checked_ ? "已点检" : "未点检";
                                                checkLbl.ForeColor = checked_ ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateFromSimulatedData()
        {
            int total = 10000 + _productionCounter * 3;
            int ok = 9800 + _productionCounter * 2;
            int ng = _rng.Next(80, 120);
            int discard = _rng.Next(70, 130);
            double yield = ok > 0 ? (double)ok / total * 100.0 : 100;
            double runtimeH = 80 + _productionCounter * 0.5;
            double cycleS = 28.0 + _rng.NextDouble() * 2 - 1;

            if (_statValueLabels.Count >= 11)
            {
                _statValueLabels[0].Text = total.ToString();
                _statValueLabels[1].Text = ok.ToString();
                _statValueLabels[2].Text = ng.ToString();
                _statValueLabels[3].Text = discard.ToString();
                _statValueLabels[4].Text = $"{yield:F1}%";
                _statValueLabels[5].Text = $"{(105 + _productionCounter):F0}h";
                _statValueLabels[6].Text = $"{runtimeH:F0}h";
                _statValueLabels[7].Text = $"{cycleS:F1}s";
            }

            _productionCounter++;

            if (_productionDgv != null && _productionDgv.Rows.Count < 200)
            {
                var now = DateTime.Now;
                string code = $"SN{now:yyyyMMdd}{_productionCounter:D5}";
                string result = _rng.NextDouble() > 0.08 ? "OK" : "NG";
                string data = (0.80 + _rng.NextDouble() * 0.1).ToString("F2");
                _productionDgv.Rows.Insert(0, now.ToString("yyyy/MM/dd HH:mm:ss"), code, result, data);
            }

            UpdateStationStatusSimulated();
            UpdateIOStatusSimulated();
            UpdateMaterialCheckSimulated();
        }

        private void UpdateStationStatusSimulated()
        {
            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var leftCol = table.GetControlFromPosition(0, 0) as Panel;
                    if (leftCol == null) continue;
                    foreach (Control cc in leftCol.Controls)
                    {
                        if (cc is TableLayoutPanel tlp)
                        {
                            var stationCard = tlp.GetControlFromPosition(0, 2) as Panel;
                            if (stationCard != null)
                            {
                                foreach (Control station in stationCard.Controls)
                                {
                                    if (station is Panel card && card.Controls.Count >= 3)
                                    {
                                        bool ok = _rng.NextDouble() > 0.1;
                                        card.BackColor = ok ? Color.FromArgb(230, 255, 230) : Color.FromArgb(255, 230, 230);
                                        if (card.Controls[1] is Label statusLbl)
                                        {
                                            statusLbl.Text = ok ? "正常" : "异常";
                                            statusLbl.ForeColor = ok ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                                        }
                                        if (card.Controls[2] is Label runLbl)
                                        {
                                            runLbl.Text = ok ? "\u25CF运行中" : "\u25CF停止";
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateIOStatusSimulated()
        {
            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var leftCol = table.GetControlFromPosition(0, 0) as Panel;
                    if (leftCol == null) continue;
                    foreach (Control cc in leftCol.Controls)
                    {
                        if (cc is TableLayoutPanel tlp)
                        {
                            var ioCard = tlp.GetControlFromPosition(0, 3) as Panel;
                            if (ioCard != null)
                            {
                                int itemIdx = 0;
                                foreach (Control io in ioCard.Controls)
                                {
                                    if (io is Panel dot && dot.Size.Width == 10)
                                    {
                                        bool ok = _rng.NextDouble() > 0.05;
                                        dot.BackColor = ok ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                                        if (ioCard.Controls.IndexOf(dot) + 1 < ioCard.Controls.Count
                                            && ioCard.Controls[ioCard.Controls.IndexOf(dot) + 1] is Label ioLabel)
                                        {
                                            string baseName = ioLabel.Text.Split(':')[0];
                                            ioLabel.Text = $"{baseName}: {(ok ? "正常" : "故障")}";
                                        }
                                        itemIdx++;
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void UpdateMaterialCheckSimulated()
        {
            foreach (Control c in Controls)
            {
                if (c is TableLayoutPanel table)
                {
                    var centerCol = table.GetControlFromPosition(1, 0) as Panel;
                    if (centerCol == null) continue;
                    foreach (Control cc in centerCol.Controls)
                    {
                        if (cc is TableLayoutPanel ctlp)
                        {
                            var matCard = ctlp.GetControlFromPosition(0, 0) as Panel;
                            if (matCard != null)
                            {
                                foreach (Control mat in matCard.Controls)
                                {
                                    if (mat is TableLayoutPanel grid)
                                    {
                                        var checkCell = grid.GetControlFromPosition(1, 0) as Panel;
                                        if (checkCell != null)
                                        {
                                            bool checked_ = _rng.NextDouble() > 0.3;
                                            checkCell.BackColor = checked_ ? Color.FromArgb(240, 255, 240) : Color.FromArgb(255, 240, 240);
                                            if (checkCell.Controls.Count >= 2 && checkCell.Controls[1] is Label checkLbl)
                                            {
                                                checkLbl.Text = checked_ ? "已点检" : "未点检";
                                                checkLbl.ForeColor = checked_ ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        public void OnActivated()
        {
            _refreshTimer?.Start();
        }

        public void OnDeactivated()
        {
            _refreshTimer?.Stop();
        }

        public void RefreshData()
        {
            _productionCounter = 0;
        }
    }
}