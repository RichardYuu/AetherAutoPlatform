using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using static Aether.Platform.UI.Modules.ControlDebugHelper;

namespace Aether.Platform.UI.Modules
{
    public class ControlDebugView : UserControl, IModuleView
    {
        public string ModuleName => "ControlDebug";
        private Panel _contentPanel;
        private readonly string[] _tabNames = { "IO", "\u8F74", "\u5DE5\u4F4D\u70B9\u4F4D", "\u901A\u8BAF\u8C03\u8BD5", "\u6A21\u62DF\u91CF\u8C03\u8BD5", "\u6599\u4ED3\u8C03\u8BD5", "\u5176\u4ED6\u786C\u4EF6\u8C03\u8BD5", "\u56DE\u5B89\u5168\u4F4D" };

        public ControlDebugView()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            BuildLayout();
            SwitchTab(0);
        }

        private void BuildLayout()
        {
            var tabBar = new Panel { Height = 38, Dock = DockStyle.Top, BackColor = Color.FromArgb(220, 225, 235), Padding = new Padding(4, 3, 4, 3) };

            int x = 4;
            for (int i = 0; i < _tabNames.Length; i++)
            {
                var t = new Label
                {
                    Text = _tabNames[i], Location = new Point(x, 4), Size = new Size(78, 28),
                    TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(200, 210, 220),
                    ForeColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8f),
                    Cursor = Cursors.Hand, Tag = i
                };
                t.Click += (s, e) => { if (s is Label l && l.Tag is int idx) SwitchTab(idx); };
                tabBar.Controls.Add(t);
                x += 82;
            }
            Controls.Add(tabBar);

            _contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 242, 245), AutoScroll = true };
            Controls.Add(_contentPanel);
        }

        private void SwitchTab(int index)
        {
            foreach (Label c in ((Panel)Controls[0]).Controls)
            {
                c.BackColor = Color.FromArgb(200, 210, 220);
                c.ForeColor = Color.FromArgb(60, 60, 60);
            }
            if (Controls[0].Controls[index] is Label active)
            {
                active.BackColor = Color.FromArgb(0, 120, 215);
                active.ForeColor = Color.White;
            }

            _contentPanel.Controls.Clear();
            UserControl page = null;
            switch (index)
            {
                case 0: page = new IOTabPage(); break;
                case 1: page = new AxisTabPage(); break;
                case 2: page = new WorkPositionTabPage(); break;
                case 3: page = new CommunicationTabPage(); break;
                case 4: page = new AnalogTabPage(); break;
                case 5: page = new StorageTabPage(); break;
                case 6: page = new OtherHardwareTabPage(); break;
                case 7: page = new SafePositionTabPage(); break;
            }
            if (page != null) { page.Dock = DockStyle.Fill; _contentPanel.Controls.Add(page); }
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData() { }
    }

    // ===== IO Tab —— 连接 HardwareService.DigitalIO =====

    public class IOTabPage : UserControl
    {
        private readonly List<Label> _ioStatusLabels = new List<Label>();

        public IOTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            table.Controls.Add(BuildIOList(), 0, 0);
            table.Controls.Add(BuildIOControl(), 1, 0);
            Controls.Add(table);
        }

        private Panel BuildIOList()
        {
            var pnl = CreateBorderedPanel("\u5DE5\u7AD91 IO\u5217\u8868");
            int y = 32;
            var ios = new[] { "\u4E0A\u4E0B\u65991\u771F\u7A7A\u68C0\u6D4B", "\u4E0A\u4E0B\u65992\u771F\u7A7A\u68C0\u6D4B", "\u4E0A\u4E0B\u6599\u6C14\u7F381\u52A8\u70B9", "\u4E0A\u4E0B\u6599\u6C14\u7F382\u52A8\u70B9" };
            foreach (var io in ios)
            {
                var row = new Panel { Location = new Point(4, y), Size = new Size(pnl.Width - 14, 26), BackColor = Color.FromArgb(245, 248, 252) };
                row.Controls.Add(new Label { Text = io, Location = new Point(4, 4), Font = new Font("Microsoft YaHei", 8.5f) });
                var stLbl = new Label { Text = "\u25CF ON", Location = new Point(row.Width - 60, 4), Size = new Size(54, 18), ForeColor = Color.Green, Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold) };
                row.Controls.Add(stLbl);
                _ioStatusLabels.Add(stLbl);
                pnl.Controls.Add(row);
                y += 28;
            }
            return pnl;
        }

        private Panel BuildIOControl()
        {
            var pnl = CreateBorderedPanel("IO \u63A7\u5236\u9762\u677F");
            pnl.AutoScroll = true;
            int y = 32;

            for (int i = 1; i <= 4; i++)
            {
                var group = new Panel { Location = new Point(4, y), Size = new Size(pnl.Width - 14, 80), BackColor = Color.FromArgb(248, 248, 252), BorderStyle = BorderStyle.FixedSingle };
                group.Controls.Add(new Label { Text = "\u4E0A\u4E0B\u6599" + i + " \u5438\u771F\u7A7A\u63A7\u5236", Location = new Point(4, 2), Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold) });

                var btn1 = new Button { Text = "\u539F\u70B9", Location = new Point(8, 24), Size = new Size(60, 26), BackColor = Color.FromArgb(0, 150, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
                btn1.FlatAppearance.BorderSize = 0;
                int idx = i;
                btn1.Click += (s, e) => ToggleIO(idx - 1, false);
                group.Controls.Add(btn1);

                var btn2 = new Button { Text = "\u52A8\u70B9", Location = new Point(74, 24), Size = new Size(60, 26), BackColor = Color.FromArgb(200, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
                btn2.FlatAppearance.BorderSize = 0;
                btn2.Click += (s, e) => ToggleIO(idx - 1, true);
                group.Controls.Add(btn2);

                pnl.Controls.Add(group);
                y += 86;
            }

            return pnl;
        }

        private void ToggleIO(int index, bool active)
        {
            var hw = GetHwService();
            if (hw == null) return;

            Task.Run(() =>
            {
                try
                {
                    var dio = hw.GetDigitalIO(index.ToString());
                    var ct = CancellationToken.None;
                    dio?.WriteAsync(active, ct).Wait();

                    BeginInvoke((Action)(() =>
                    {
                        if (index < _ioStatusLabels.Count)
                        {
                            _ioStatusLabels[index].Text = active ? "\u25CF ON" : "\u25CF OFF";
                            _ioStatusLabels[index].ForeColor = active ? Color.Green : Color.FromArgb(200, 60, 60);
                        }
                    }));

                    Log("IO", $"IO[{index}] = {(active ? "ON" : "OFF")}");
                }
                catch (Exception ex) { Log("IO", $"出错: {ex.Message}"); }
            });
        }

        private Panel CreateBorderedPanel(string title)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3) };
            pnl.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });
            return pnl;
        }
    }

    // ===== 轴 Tab =====

    public class AxisTabPage : UserControl
    {
        private readonly List<Label> _posLabels = new List<Label>();
        private readonly List<Label> _statusLabels = new List<Label>();
        private readonly string[] _axisIds = { "X1", "Z1", "X2", "Z2", "Z5", "Z6" };

        public AxisTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(4), BackColor = Color.Transparent };
            var names = new[] { "\u5DE6X\u8F74", "\u9F99\u95E8Z1\u8F74", "\u53D6\u6599X2\u8F74", "\u9F99\u95E8Z2\u8F74", "\u4E0A\u6599Z5\u8F74", "NG\u5DE5\u4F4DZ6\u8F74" };
            for (int i = 0; i < 6; i++)
                grid.Controls.Add(BuildAxisCard(names[i], i), i % 3, i / 3);
            Controls.Add(grid);
        }

        private Panel BuildAxisCard(string name, int idx)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Padding = new Padding(8) };

            card.Controls.Add(new Label { Text = name, Location = new Point(6, 6), Size = new Size(120, 24), Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            var modePnl = new Panel { Location = new Point(6, 34), Size = new Size(200, 26) };
            var rbJog = new RadioButton { Text = "\u70B9\u52A8", Location = new Point(0, 2), Size = new Size(50, 20), Checked = true, Font = new Font("Microsoft YaHei", 8f) };
            var rbFast = new RadioButton { Text = "\u9AD8\u901F", Location = new Point(56, 2), Size = new Size(50, 20), Font = new Font("Microsoft YaHei", 8f) };
            modePnl.Controls.Add(rbJog);
            modePnl.Controls.Add(rbFast);
            card.Controls.Add(modePnl);

            var resetBtn = new Button { Text = "\u590D\u4F4D", Location = new Point(170, 32), Size = new Size(54, 24), BackColor = Color.FromArgb(211, 47, 47), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            resetBtn.FlatAppearance.BorderSize = 0;
            resetBtn.Click += (s, e) => ResetAxis(idx);
            card.Controls.Add(resetBtn);

            var chkEnable = new CheckBox { Text = "\u4F7F\u80FD", Location = new Point(6, 64), Size = new Size(56, 20), Font = new Font("Microsoft YaHei", 8f) };
            card.Controls.Add(chkEnable);

            var posLbl = new Label { Text = "+000.000 mm", Location = new Point(70, 64), Size = new Size(120, 22), Font = new Font("Consolas", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 160) };
            _posLabels.Add(posLbl);
            card.Controls.Add(posLbl);

            var stLbl = new Label { Text = "\u6B63\u5E38", Location = new Point(6, 90), Size = new Size(48, 22), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 8f) };
            _statusLabels.Add(stLbl);
            card.Controls.Add(stLbl);

            var limitPnl = new Panel { Location = new Point(6, 118), Size = new Size(card.Width - 14, 32) };
            var negBtn = new Button { Text = "\u8D1F\u6781\u9650", Location = new Point(0, 4), Size = new Size(54, 22), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 7f) };
            negBtn.Click += (s, e) => MoveAxis(idx, -999, () => rbFast.Checked);
            limitPnl.Controls.Add(negBtn);

            var homeBtn = new Button { Text = "\u539F\u70B9", Location = new Point(60, 4), Size = new Size(46, 22), BackColor = Color.FromArgb(180, 220, 255), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 7f) };
            homeBtn.Click += (s, e) => HomeAxis(idx);
            limitPnl.Controls.Add(homeBtn);

            var posBtn = new Button { Text = "\u6B63\u6781\u9650", Location = new Point(112, 4), Size = new Size(54, 22), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 7f) };
            posBtn.Click += (s, e) => MoveAxis(idx, 999, () => rbFast.Checked);
            limitPnl.Controls.Add(posBtn);
            card.Controls.Add(limitPnl);

            var dirPnl = new Panel { Location = new Point(6, 154), Size = new Size(card.Width - 14, 32) };
            var negDirBtn = new Button { Text = "\u8D1F\u5411", Location = new Point(0, 4), Size = new Size(72, 22), BackColor = Color.FromArgb(100, 160, 220), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            negDirBtn.FlatAppearance.BorderSize = 0;
            negDirBtn.Click += (s, e) => { var fast = rbFast.Checked; MoveAxis(idx, fast ? -10 : -1, () => fast); };
            dirPnl.Controls.Add(negDirBtn);

            var posDirBtn = new Button { Text = "\u6B63\u5411", Location = new Point(78, 4), Size = new Size(72, 22), BackColor = Color.FromArgb(220, 100, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            posDirBtn.FlatAppearance.BorderSize = 0;
            posDirBtn.Click += (s, e) => { var fast = rbFast.Checked; MoveAxis(idx, fast ? 10 : 1, () => fast); };
            dirPnl.Controls.Add(posDirBtn);
            card.Controls.Add(dirPnl);

            return card;
        }

        private void MoveAxis(int idx, double pos, Func<bool> isFast)
        {
            var hw = GetHwService();
            if (hw == null || idx >= _axisIds.Length) return;

            double speed = isFast() ? 50 : 10;
            Task.Run(() =>
            {
                try
                {
                    var axis = hw.GetAxis(_axisIds[idx]);
                    var ct = CancellationToken.None;
                    axis?.MoveAbsAsync(pos, speed, ct).Wait();

                    RefreshAxisPosition(idx, hw);
                    Log("Axis", $"\u8F74[{_axisIds[idx]}] \u79FB\u52A8\u5230 {pos:F1} mm");
                }
                catch (Exception ex) { Log("Axis", $"出错: {ex.Message}"); }
            });
        }

        private void HomeAxis(int idx)
        {
            var hw = GetHwService();
            if (hw == null || idx >= _axisIds.Length) return;

            Task.Run(() =>
            {
                try
                {
                    var axis = hw.GetAxis(_axisIds[idx]);
                    axis?.HomeAsync(CancellationToken.None).Wait();
                    RefreshAxisPosition(idx, hw);
                    Log("Axis", $"\u8F74[{_axisIds[idx]}] \u56DE\u539F\u70B9");
                }
                catch (Exception ex) { Log("Axis", $"出错: {ex.Message}"); }
            });
        }

        private void ResetAxis(int idx)
        {
            var hw = GetHwService();
            if (hw == null || idx >= _axisIds.Length) return;

            Task.Run(() =>
            {
                try
                {
                    var axis = hw.GetAxis(_axisIds[idx]);
                    axis?.StopAsync(CancellationToken.None).Wait();
                    BeginInvoke((Action)(() =>
                    {
                        if (idx < _statusLabels.Count)
                        {
                            _statusLabels[idx].Text = "\u5FA9\u4F4D";
                            _statusLabels[idx].BackColor = Color.FromArgb(200, 120, 20);
                        }
                    }));
                    Log("Axis", $"\u8F74[{_axisIds[idx]}] \u590D\u4F4D");
                }
                catch (Exception ex) { Log("Axis", $"出错: {ex.Message}"); }
            });
        }

        private void RefreshAxisPosition(int idx, IHardwareService hw)
        {
            try
            {
                var axis = hw.GetAxis(_axisIds[idx]);
                if (idx < _posLabels.Count)
                {
                    double pos = axis?.CurrentPosition ?? 0;
                    BeginInvoke((Action)(() =>
                    {
                        _posLabels[idx].Text = $"{(pos >= 0 ? "+" : "")}{pos:F3} mm";
                        if (idx < _statusLabels.Count)
                        {
                            _statusLabels[idx].Text = "\u6B63\u5E38";
                            _statusLabels[idx].BackColor = Color.FromArgb(76, 175, 80);
                        }
                    }));
                }
            }
            catch { }
        }
    }

    // ===== 工作点位 Tab =====

    public class WorkPositionTabPage : UserControl
    {
        public WorkPositionTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(4), BackColor = Color.Transparent };
            var stations = new[] { "\u53D6\u6599\u4F4D", "\u4E0A\u6599\u4F4D", "\u6D4B\u8BD5\u4F4D1", "\u6D4B\u8BD5\u4F4D2", "NG\u4F4D", "\u4E0B\u6599\u4F4D" };
            for (int i = 0; i < 6; i++)
                grid.Controls.Add(BuildPositionCard(stations[i], i), i % 2, i / 2);
            Controls.Add(grid);
        }

        private Panel BuildPositionCard(string name, int index)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Padding = new Padding(8) };

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(240, 244, 250) };
            titleBar.Controls.Add(new Label { Text = name, Location = new Point(6, 5), Size = new Size(100, 20), Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });
            titleBar.Controls.Add(new Label { Text = "\u25CF \u5F85\u673A", Location = new Point(120, 5), Size = new Size(60, 20), ForeColor = index < 2 ? Color.Green : Color.Gray, Font = new Font("Microsoft YaHei", 8f) });
            card.Controls.Add(titleBar);

            var infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            int y = 4;
            AddKVRow(infoPanel, "\u5F53\u524D\u4EF6\u53F7:", "----", ref y);
            AddKVRow(infoPanel, "\u5F53\u524D\u6599\u76D8:", "\u76D81", ref y);
            AddKVRow(infoPanel, "\u72B6\u6001:", "\u5F85\u673A", ref y);
            AddKVRow(infoPanel, "\u538B\u529B:", "0.00 MPa", ref y);
            AddKVRow(infoPanel, "\u6E29\u5EA6:", "25.0 \u2103", ref y);
            card.Controls.Add(infoPanel);

            var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.Transparent };
            var goBtn = new Button { Text = "\u524D\u5F80", Location = new Point(4, 2), Size = new Size(60, 24), BackColor = Color.FromArgb(0, 140, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            goBtn.FlatAppearance.BorderSize = 0;
            goBtn.Click += (s, e) => Log("\u5DE5\u4F4D\u70B9\u4F4D", $"{name} \u524D\u5F80\u6307\u4EE4\u5DF2\u53D1\u9001");
            btnBar.Controls.Add(goBtn);

            var homeBtn = new Button { Text = "\u56DE\u539F\u70B9", Location = new Point(68, 2), Size = new Size(60, 24), BackColor = Color.FromArgb(200, 100, 20), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            homeBtn.FlatAppearance.BorderSize = 0;
            homeBtn.Click += (s, e) => Log("\u5DE5\u4F4D\u70B9\u4F4D", $"{name} \u56DE\u539F\u70B9\u6307\u4EE4\u5DF2\u53D1\u9001");
            btnBar.Controls.Add(homeBtn);
            card.Controls.Add(btnBar);

            return card;
        }

        private void AddKVRow(Panel parent, string key, string value, ref int y)
        {
            var row = new Panel { Location = new Point(4, y), Size = new Size(parent.Width - 12, 22) };
            row.Controls.Add(new Label { Text = key, Location = new Point(0, 2), Size = new Size(70, 18), Font = new Font("Microsoft YaHei", 8f), ForeColor = Color.Gray });
            row.Controls.Add(new Label { Text = value, Location = new Point(72, 2), Size = new Size(120, 18), Font = new Font("Microsoft YaHei", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 160) });
            parent.Controls.Add(row);
            y += 22;
        }
    }

    // ===== 通讯调试 Tab —— 连接 HardwareService.Plc =====

    public class CommunicationTabPage : UserControl
    {
        private TextBox _txtAddr;
        private TextBox _txtValue;
        private Label _lblResult;
        private Label _lblLog1, _lblLog2, _lblLog3;

        public CommunicationTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            table.Controls.Add(BuildLeftComms(), 0, 0);
            table.Controls.Add(BuildRightComms(), 1, 0);
            Controls.Add(table);
        }

        private Panel BuildLeftComms()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3), AutoScroll = true };
            pnl.Controls.Add(new Label { Text = "PLC / \u4E32\u53E3\u901A\u4FE1", Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            int y = 32;
            var comms = new (string Name, string Status, Color C)[] {
                ("PLC \u4E3B\u7AD9 (192.168.1.100)", "\u5DF2\u8FDE\u63A5", Color.Green),
                ("COM3 \u626B\u7801\u5668 (9600,N,8,1)", "\u5DF2\u8FDE\u63A5", Color.Green),
                ("COM4 \u5FAE\u538B\u4F20\u611F\u5668", "\u5DF2\u8FDE\u63A5", Color.Green),
                ("Modbus TCP \u6E29\u63A7\u5668", "\u5DF2\u8FDE\u63A5", Color.Green),
                ("COM5 \u6FC0\u5149\u6D4B\u8DDD", "\u672A\u8FDE\u63A5", Color.Red),
                ("EtherNet/IP \u89C6\u89C9\u7CFB\u7EDF", "\u5DF2\u8FDE\u63A5", Color.Green),
            };
            foreach (var comm in comms)
            {
                var row = new Panel { Location = new Point(4, y), Size = new Size(pnl.Width - 14, 32), BackColor = Color.FromArgb(245, 248, 252), BorderStyle = BorderStyle.FixedSingle };
                row.Controls.Add(new Label { Text = comm.Name, Location = new Point(6, 6), Font = new Font("Microsoft YaHei", 8f) });
                row.Controls.Add(new Label { Text = "\u25CF " + comm.Status, Location = new Point(row.Width - 90, 6), Size = new Size(82, 18), ForeColor = comm.C, Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight });
                pnl.Controls.Add(row);
                y += 36;
            }
            return pnl;
        }

        private Panel BuildRightComms()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3) };
            pnl.Controls.Add(new Label { Text = "\u901A\u4FE1\u6D4B\u8BD5", Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            var testPanel = new Panel { Location = new Point(4, 34), Size = new Size(pnl.Width - 12, 140), BackColor = Color.FromArgb(248, 249, 252), BorderStyle = BorderStyle.FixedSingle };

            testPanel.Controls.Add(new Label { Text = "\u8BFB\u5199\u6D4B\u8BD5\u5730\u5740 (PLC):", Location = new Point(8, 10), Font = new Font("Microsoft YaHei", 8.5f) });
            _txtAddr = new TextBox { Text = "D100", Location = new Point(160, 8), Size = new Size(80, 22) };
            testPanel.Controls.Add(_txtAddr);

            testPanel.Controls.Add(new Label { Text = "\u5199\u5165\u503C:", Location = new Point(8, 38), Font = new Font("Microsoft YaHei", 8.5f) });
            _txtValue = new TextBox { Text = "0", Location = new Point(160, 36), Size = new Size(80, 22) };
            testPanel.Controls.Add(_txtValue);

            var readBtn = new Button { Text = "\u8BFB\u53D6", Location = new Point(8, 68), Size = new Size(70, 26), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            readBtn.FlatAppearance.BorderSize = 0;
            readBtn.Click += (s, e) => ReadPlc();
            testPanel.Controls.Add(readBtn);

            var writeBtn = new Button { Text = "\u5199\u5165", Location = new Point(84, 68), Size = new Size(70, 26), BackColor = Color.FromArgb(200, 100, 20), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            writeBtn.FlatAppearance.BorderSize = 0;
            writeBtn.Click += (s, e) => WritePlc();
            testPanel.Controls.Add(writeBtn);

            var pingBtn = new Button { Text = "Ping \u6D4B\u8BD5", Location = new Point(160, 68), Size = new Size(80, 26), BackColor = Color.FromArgb(100, 160, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            pingBtn.FlatAppearance.BorderSize = 0;
            pingBtn.Click += (s, e) => PingPlc();
            testPanel.Controls.Add(pingBtn);

            _lblResult = new Label { Text = "\u7ED3\u679C: ----", Location = new Point(8, 102), Size = new Size(200, 20), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.FromArgb(0, 80, 160) };
            testPanel.Controls.Add(_lblResult);

            pnl.Controls.Add(testPanel);

            var logPanel = new Panel { Location = new Point(4, 182), Size = new Size(pnl.Width - 12, 120), BackColor = Color.FromArgb(30, 30, 35), BorderStyle = BorderStyle.FixedSingle };
            logPanel.Controls.Add(new Label { Text = "\u901A\u4FE1\u65E5\u5FD7", ForeColor = Color.FromArgb(0, 200, 80), Location = new Point(4, 2), Font = new Font("Consolas", 8f) });
            _lblLog1 = new Label { Text = "[--:--:--] PLC\u8FDE\u63A5\u6B63\u5E38", ForeColor = Color.FromArgb(180, 220, 180), Location = new Point(4, 20), Font = new Font("Consolas", 8f) };
            _lblLog2 = new Label { Text = "[--:--:--] \u7B49\u5F85\u64CD\u4F5C...", ForeColor = Color.FromArgb(180, 220, 180), Location = new Point(4, 36), Font = new Font("Consolas", 8f) };
            _lblLog3 = new Label { Text = "", ForeColor = Color.FromArgb(180, 220, 180), Location = new Point(4, 52), Font = new Font("Consolas", 8f) };
            logPanel.Controls.Add(_lblLog1);
            logPanel.Controls.Add(_lblLog2);
            logPanel.Controls.Add(_lblLog3);
            pnl.Controls.Add(logPanel);

            return pnl;
        }

        private void ReadPlc()
        {
            var hw = GetHwService();
            if (hw == null) return;
            var addr = _txtAddr.Text.Trim();
            if (string.IsNullOrEmpty(addr)) return;

            Task.Run(() =>
            {
                try
                {
                    var plc = hw.GetPlc();
                    var ct = CancellationToken.None;
                    int value = plc?.ReadWordAsync(addr, ct).Result ?? 0;
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"结果: {value}";
                        _lblResult.ForeColor = Color.FromArgb(0, 120, 40);
                    }));
                    Log("PLC", $"\u8BFB\u53D6 {addr} = {value}");
                }
                catch (Exception ex)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"错误: {ex.Message}";
                        _lblResult.ForeColor = Color.Red;
                    }));
                    Log("PLC", $"\u8BFB\u53D6\u5931\u8D25: {ex.Message}");
                }
            });
        }

        private void WritePlc()
        {
            var hw = GetHwService();
            if (hw == null) return;
            var addr = _txtAddr.Text.Trim();
            if (string.IsNullOrEmpty(addr) || !int.TryParse(_txtValue.Text, out int val)) return;

            Task.Run(() =>
            {
                try
                {
                    var plc = hw.GetPlc();
                    var ct = CancellationToken.None;
                    plc?.WriteWordAsync(addr, val, ct).Wait();
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"写入成功: {addr}={val}";
                        _lblResult.ForeColor = Color.FromArgb(0, 120, 40);
                    }));
                    Log("PLC", $"\u5199\u5165 {addr} = {val}");
                }
                catch (Exception ex)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"错误: {ex.Message}";
                        _lblResult.ForeColor = Color.Red;
                    }));
                    Log("PLC", $"\u5199\u5165\u5931\u8D25: {ex.Message}");
                }
            });
        }

        private void PingPlc()
        {
            var hw = GetHwService();
            if (hw == null) return;

            Task.Run(() =>
            {
                try
                {
                    var plc = hw.GetPlc();
                    var ct = CancellationToken.None;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    plc?.ReadWordAsync("D0", ct).Wait();
                    sw.Stop();
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"Ping: {sw.ElapsedMilliseconds}ms";
                        _lblResult.ForeColor = Color.FromArgb(0, 120, 40);
                    }));
                    Log("PLC", $"Ping \u54CD\u5E94: {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _lblResult.Text = $"Ping 超时";
                        _lblResult.ForeColor = Color.Red;
                    }));
                    Log("PLC", $"Ping \u8D85\u65F6: {ex.Message}");
                }
            });
        }
    }

    // ===== 模拟量 Tab =====

    public class AnalogTabPage : UserControl
    {
        public AnalogTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(4), BackColor = Color.Transparent };
            var channels = new (string Name, string Value, string Unit)[] {
                ("CH1 \u771F\u7A7A\u5EA6", "-50.2", "kPa"), ("CH2 \u6C14\u538B", "0.50", "MPa"),
                ("CH3 \u6D41\u91CF", "12.5", "L/min"), ("CH4 \u6E29\u5EA6", "25.3", "\u2103"),
                ("CH5 \u538B\u5DEE", "0.02", "MPa"), ("CH6 \u8D1F\u8F7D\u7535\u6D41", "1.25", "A"),
            };
            for (int i = 0; i < 6; i++)
                grid.Controls.Add(BuildAnalogCard(channels[i].Name, channels[i].Value, channels[i].Unit, i), i % 3, i / 3);
            Controls.Add(grid);
        }

        private Panel BuildAnalogCard(string name, string value, string unit, int idx)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Padding = new Padding(8) };
            card.Controls.Add(new Label { Text = name, Location = new Point(6, 6), Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            var valLbl = new Label { Text = value, Location = new Point(6, 34), Size = new Size(140, 36), Font = new Font("Consolas", 22f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 160) };
            card.Controls.Add(valLbl);
            card.Controls.Add(new Label { Text = unit, Location = new Point(146, 44), Size = new Size(40, 20), Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.Gray });

            var bar = new Panel { Location = new Point(6, 74), Size = new Size(card.Width - 16, 10), BackColor = Color.FromArgb(220, 225, 235) };
            var fill = new Panel { Size = new Size(Convert.ToInt32((card.Width - 16) * 0.65), 10), BackColor = Color.FromArgb(33, 150, 243) };
            bar.Controls.Add(fill);
            card.Controls.Add(bar);

            var readBtn = new Button { Text = "\u8BFB\u53D6", Location = new Point(6, 90), Size = new Size(50, 24), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 7f) };
            readBtn.FlatAppearance.BorderSize = 0;
            readBtn.Click += (s, e) => { valLbl.Text = (new Random().NextDouble() * 20 - 10).ToString("F2"); Log("\u6A21\u62DF\u91CF", $"{name} \u8BFB\u53D6\u6210\u529F"); };
            card.Controls.Add(readBtn);

            return card;
        }
    }

    // ===== 料仓调试 Tab =====

    public class StorageTabPage : UserControl
    {
        public StorageTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            table.Controls.Add(BuildTrayListView(), 0, 0);
            table.Controls.Add(BuildStorageControl(), 1, 0);
            Controls.Add(table);
        }

        private Panel BuildTrayListView()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3), AutoScroll = true };
            pnl.Controls.Add(new Label { Text = "\u6599\u76D8\u72B6\u6001", Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            var trayTabs = new Panel { Location = new Point(4, 30), Size = new Size(pnl.Width - 12, 28) };
            for (int i = 1; i <= 4; i++)
            {
                var t = new Label { Text = "\u76D8" + i, Location = new Point((i - 1) * 54, 2), Size = new Size(48, 22), TextAlign = ContentAlignment.MiddleCenter, BackColor = i == 1 ? Color.FromArgb(0, 120, 215) : Color.FromArgb(200, 210, 220), ForeColor = i == 1 ? Color.White : Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8f), Cursor = Cursors.Hand };
                trayTabs.Controls.Add(t);
            }
            pnl.Controls.Add(trayTabs);

            var grid = new TableLayoutPanel { Location = new Point(4, 62), Size = new Size(pnl.Width - 12, 200), ColumnCount = 6, RowCount = 4, BackColor = Color.Transparent };
            string[] states = { "\u7A7A", "\u5F85\u786E\u8BA4", "\u5F85\u786E\u8BA4", "\u4F5C\u4E1A\u4E2D", "\u5B8C\u6210", "\u5B8C\u6210", "\u7A7A", "\u5F85\u786E\u8BA4", "\u5F85\u786E\u8BA4", "\u5F85\u4F5C\u4E1A", "\u4F5C\u4E1A\u4E2D", "\u786E\u8BA4", "\u7A7A", "\u7A7A", "\u5F85\u786E\u8BA4", "\u5F85\u4F5C\u4E1A", "\u4F5C\u4E1A\u4E2D", "\u5B8C\u6210", "\u7A7A", "\u7A7A", "\u7A7A", "\u5F85\u786E\u8BA4", "\u5F85\u4F5C\u4E1A", "\u5B8C\u6210" };
            var colorMap = new Dictionary<string, Color> { {"\u7A7A", Color.FromArgb(220,220,220)}, {"\u5F85\u786E\u8BA4", Color.FromArgb(255,235,180)}, {"\u786E\u8BA4", Color.FromArgb(180,230,255)}, {"\u5F85\u4F5C\u4E1A", Color.FromArgb(255,200,140)}, {"\u4F5C\u4E1A\u4E2D", Color.FromArgb(33,150,243)}, {"\u5B8C\u6210", Color.FromArgb(76,175,80)} };
            for (int i = 0; i < 24; i++)
            {
                var cell = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = colorMap.TryGetValue(states[i], out var c) ? c : Color.Gray, Margin = new Padding(1) };
                cell.Controls.Add(new Label { Text = (i / 6 + 1) + "-" + (i % 6 + 1), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 6.5f) });
                grid.Controls.Add(cell, i % 6, i / 6);
            }
            pnl.Controls.Add(grid);
            return pnl;
        }

        private Panel BuildStorageControl()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3) };
            pnl.Controls.Add(new Label { Text = "\u6599\u4ED3\u63A7\u5236", Dock = DockStyle.Top, Height = 26, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            int y = 34;
            var ctrlItems = new (string Label, string Status, Color C)[] {
                ("\u5DE6\u6599\u4ED3\u63A8\u6599\u6C14\u7F38", "\u539F\u70B9", Color.Green), ("\u5DE6\u6599\u4ED3\u5347\u964D\u6C14\u7F38", "\u539F\u70B9", Color.Green),
                ("\u53F3\u6599\u4ED3\u63A8\u6599\u6C14\u7F38", "\u52A8\u70B9", Color.Orange), ("\u53F3\u6599\u4ED3\u5347\u964D\u6C14\u7F38", "\u539F\u70B9", Color.Green),
                ("\u6599\u76D8\u5B9A\u4F4D\u4F20\u611F\u5668", "ON", Color.Green), ("\u6599\u76D8\u786E\u8BA4\u4F20\u611F\u5668", "OFF", Color.Red),
            };
            foreach (var item in ctrlItems)
            {
                var row = new Panel { Location = new Point(4, y), Size = new Size(pnl.Width - 12, 30), BackColor = Color.FromArgb(245, 248, 252), BorderStyle = BorderStyle.FixedSingle };
                row.Controls.Add(new Label { Text = item.Label, Location = new Point(6, 5), Font = new Font("Microsoft YaHei", 8.5f) });
                row.Controls.Add(new Label { Text = "\u25CF " + item.Status, Location = new Point(row.Width - 80, 5), Size = new Size(72, 18), ForeColor = item.C, Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight });
                pnl.Controls.Add(row);
                y += 34;
            }

            var confirmBtn = new Button { Text = "\u6599\u4ED3\u786E\u8BA4", Location = new Point(4, y + 8), Size = new Size(100, 30), BackColor = Color.FromArgb(0, 140, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
            confirmBtn.FlatAppearance.BorderSize = 0;
            confirmBtn.Click += (s, e) => Log("\u6599\u4ED3", "\u6599\u4ED3\u786E\u8BA4\u6307\u4EE4\u5DF2\u53D1\u9001");
            pnl.Controls.Add(confirmBtn);

            var resetBtn = new Button { Text = "\u6599\u4ED3\u590D\u4F4D", Location = new Point(110, y + 8), Size = new Size(100, 30), BackColor = Color.FromArgb(211, 47, 47), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
            resetBtn.FlatAppearance.BorderSize = 0;
            resetBtn.Click += (s, e) => Log("\u6599\u4ED3", "\u6599\u4ED3\u590D\u4F4D\u6307\u4EE4\u5DF2\u53D1\u9001");
            pnl.Controls.Add(resetBtn);

            return pnl;
        }
    }

    // ===== 其他硬件调试 Tab =====

    public class OtherHardwareTabPage : UserControl
    {
        public OtherHardwareTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(4), BackColor = Color.Transparent };
            var devices = new (string Name, string Value, string Unit, string Status)[] {
                ("\u7535\u5B50\u79E4", "125.3", "g", "\u6B63\u5E38"), ("\u6FC0\u5149\u6D4B\u8DDD", "45.2", "mm", "\u6B63\u5E38"),
                ("\u6E29\u63A7\u5668", "25.0", "\u2103", "\u6B63\u5E38"), ("\u5FAE\u538B\u4F20\u611F\u5668", "0.50", "MPa", "\u6B63\u5E38"),
                ("\u6C14\u52A8\u6BD4\u4F8B\u9600", "45.0", "%", "\u6B63\u5E38"), ("\u66DD\u5149\u8BA1", "1200", "lux", "\u6B63\u5E38"),
            };
            for (int i = 0; i < 6; i++)
                grid.Controls.Add(BuildDeviceCard(devices[i], i), i % 3, i / 3);
            Controls.Add(grid);
        }

        private Panel BuildDeviceCard((string Name, string Value, string Unit, string Status) dev, int idx)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Padding = new Padding(8) };
            card.Controls.Add(new Label { Text = dev.Name, Location = new Point(6, 6), Size = new Size(120, 22), Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });

            var statusBadge = new Label { Text = dev.Status, Location = new Point(138, 6), Size = new Size(44, 20), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 7f) };
            card.Controls.Add(statusBadge);

            var valLbl = new Label { Text = dev.Value, Location = new Point(6, 34), Size = new Size(100, 30), Font = new Font("Consolas", 18f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 160) };
            card.Controls.Add(valLbl);
            card.Controls.Add(new Label { Text = dev.Unit, Location = new Point(106, 42), Size = new Size(40, 18), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.Gray });

            var readBtn = new Button { Text = "\u8BFB\u53D6", Location = new Point(6, 68), Size = new Size(55, 24), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            readBtn.FlatAppearance.BorderSize = 0;
            readBtn.Click += (s, e) => { valLbl.Text = (new Random().Next(50, 150)).ToString("F1"); Log(dev.Name, "\u8BFB\u53D6\u6210\u529F"); };
            card.Controls.Add(readBtn);

            var zeroBtn = new Button { Text = "\u5F52\u96F6", Location = new Point(66, 68), Size = new Size(55, 24), BackColor = Color.FromArgb(100, 160, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            zeroBtn.FlatAppearance.BorderSize = 0;
            zeroBtn.Click += (s, e) => { valLbl.Text = "0.0"; Log(dev.Name, "\u5F52\u96F6\u6210\u529F"); };
            card.Controls.Add(zeroBtn);

            var calBtn = new Button { Text = "\u6807\u5B9A", Location = new Point(126, 68), Size = new Size(55, 24), BackColor = Color.FromArgb(200, 120, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f) };
            calBtn.FlatAppearance.BorderSize = 0;
            calBtn.Click += (s, e) => { valLbl.Text = "100.0"; Log(dev.Name, "\u6807\u5B9A\u6210\u529F"); };
            card.Controls.Add(calBtn);

            return card;
        }
    }

    // ===== 回安全位 Tab =====

    public class SafePositionTabPage : UserControl
    {
        public SafePositionTabPage()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(6);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10) };
            pnl.Controls.Add(new Label { Text = "\u56DE\u5B89\u5168\u4F4D", Location = new Point(12, 12), Size = new Size(300, 30), Font = new Font("Microsoft YaHei", 16f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140) });
            pnl.Controls.Add(new Label { Text = "\u5C06\u6240\u6709\u8F74\u79FB\u52A8\u5230\u5B89\u5168\u4F4D\u7F6E\uff0c\u786E\u4FDD\u64CD\u4F5C\u4EBA\u5458\u5B89\u5168\u3002", Location = new Point(12, 46), Size = new Size(400, 24), Font = new Font("Microsoft YaHei", 10f), ForeColor = Color.FromArgb(120, 120, 120) });

            var axes = new[] { "\u5DE6X\u8F74", "\u9F99\u95E8Z1\u8F74", "\u53D6\u6599X2\u8F74", "\u9F99\u95E8Z2\u8F74", "\u4E0A\u6599Z5\u8F74", "NG\u4F4DZ6\u8F74" };
            var axisIds = new[] { "X1", "Z1", "X2", "Z2", "Z5", "Z6" };
            int y = 80;
            for (int i = 0; i < axes.Length; i++)
            {
                int idx = i;
                var row = new Panel { Location = new Point(12, y), Size = new Size(500, 36), BackColor = Color.FromArgb(245, 248, 252), BorderStyle = BorderStyle.FixedSingle };
                row.Controls.Add(new Label { Text = axes[i], Location = new Point(8, 8), Size = new Size(100, 20), Font = new Font("Microsoft YaHei", 10f) });
                row.Controls.Add(new Label { Text = "\u5F53\u524D: +125.3 mm", Location = new Point(120, 8), Size = new Size(130, 20), Font = new Font("Consolas", 9f), ForeColor = Color.FromArgb(0, 80, 160) });
                row.Controls.Add(new Label { Text = "\u2192 \u5B89\u5168\u4F4D: 0.0 mm", Location = new Point(260, 8), Size = new Size(130, 20), Font = new Font("Consolas", 9f), ForeColor = Color.FromArgb(76, 175, 80) });
                var goBtn = new Button { Text = "\u56DE\u4F4D", Location = new Point(400, 5), Size = new Size(80, 26), BackColor = Color.FromArgb(33, 150, 243), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
                goBtn.FlatAppearance.BorderSize = 0;
                goBtn.Click += (s, e) => SafeHomeAxis(axisIds[idx], axes[idx]);
                row.Controls.Add(goBtn);
                pnl.Controls.Add(row);
                y += 42;
            }

            var allBtn = new Button { Text = "\u4E00\u952E\u5168\u90E8\u56DE\u5B89\u5168\u4F4D", Location = new Point(12, y + 10), Size = new Size(200, 40), BackColor = Color.FromArgb(211, 47, 47), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold) };
            allBtn.FlatAppearance.BorderSize = 0;
            allBtn.Click += (s, e) => SafeHomeAll(axisIds);
            pnl.Controls.Add(allBtn);

            Controls.Add(pnl);
        }

        private void SafeHomeAxis(string axisId, string name)
        {
            var hw = GetHwService();
            if (hw == null) return;

            Task.Run(() =>
            {
                try
                {
                    hw.GetAxis(axisId)?.MoveAbsAsync(0, 20, CancellationToken.None).Wait();
                    Log("\u5B89\u5168\u4F4D", $"{name} \u5DF2\u56DE\u5B89\u5168\u4F4D");
                }
                catch (Exception ex) { Log("\u5B89\u5168\u4F4D", $"{name} \u5931\u8D25: {ex.Message}"); }
            });
        }

        private void SafeHomeAll(string[] axisIds)
        {
            var hw = GetHwService();
            if (hw == null) return;

            Task.Run(() =>
            {
                var tasks = new List<Task>();
                foreach (var id in axisIds)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try { hw.GetAxis(id)?.MoveAbsAsync(0, 20, CancellationToken.None).Wait(); }
                        catch { }
                    }));
                }
                Task.WaitAll(tasks.ToArray());
                Log("\u5B89\u5168\u4F4D", "\u6240\u6709\u8F74\u5DF2\u56DE\u5B89\u5168\u4F4D");
            });
        }
    }

    // ===== 公共辅助 =====

    internal static class ControlDebugHelper
    {
        public static IHardwareService GetHwService()
        {
            return AppBootstrap.Instance?.HardwareService;
        }

        public static void Log(string category, string message)
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            var fullMsg = $"[{now}] {message}";
            AppBootstrap.Instance?.OnSystemLog?.Invoke(category, fullMsg);
        }
    }
}