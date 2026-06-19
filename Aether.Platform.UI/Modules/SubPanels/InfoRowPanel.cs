using System.Drawing;
using System.Windows.Forms;

namespace Aether.Platform.UI.Modules.SubPanels
{
    public class InfoRowPanel : UserControl
    {
        public InfoRowPanel()
        {
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 4);
            InitializeUI();
        }

        private void InitializeUI()
        {
            var items = new[]
            {
                ("总数", "12,580", Color.FromArgb(52, 152, 219)),
                ("OK", "12,420", Color.FromArgb(39, 174, 96)),
                ("NG", "160", Color.FromArgb(231, 76, 60)),
                ("良率", "98.72%", Color.FromArgb(39, 174, 96)),
                ("OEE", "87.5%", Color.FromArgb(155, 89, 182)),
                ("当前件号", "NGL13-ABC123", Color.FromArgb(44, 62, 80)),
            };

            int x = 0;
            foreach (var (label, value, color) in items)
            {
                var card = new Panel
                {
                    Size = new Size(200, 58),
                    Location = new Point(x + 12, 6),
                    BackColor = Color.FromArgb(248, 249, 250),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var titleLabel = new Label
                {
                    Text = label,
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(127, 140, 141),
                    Location = new Point(10, 6),
                    Size = new Size(180, 18),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var valueLabel = new Label
                {
                    Text = value,
                    Font = new Font("Microsoft YaHei", 18f, FontStyle.Bold),
                    ForeColor = color,
                    Location = new Point(10, 24),
                    Size = new Size(180, 30),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                card.Controls.Add(titleLabel);
                card.Controls.Add(valueLabel);
                Controls.Add(card);
                x += 212;
            }
        }
    }

    public class ProductionCurvePanel : UserControl
    {
        public ProductionCurvePanel()
        {
            BackColor = Color.White;
            Margin = new Padding(0, 0, 4, 4);
            var lbl = new Label
            {
                Text = "生产趋势曲线",
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(12, 10),
                Size = new Size(200, 24)
            };
            var chartArea = new Panel
            {
                BackColor = Color.FromArgb(248, 249, 250),
                Location = new Point(12, 40),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            chartArea.Paint += DrawMockChart;
            Controls.Add(lbl);
            Controls.Add(chartArea);
        }

        private void DrawMockChart(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = ((Panel)sender).ClientRectangle;
            using (var pen = new Pen(Color.FromArgb(52, 152, 219), 2f))
            {
                var points = new Point[]
                {
                    new Point(20, rect.Height - 30),
                    new Point(60, rect.Height - 50),
                    new Point(100, rect.Height - 35),
                    new Point(140, rect.Height - 55),
                    new Point(180, rect.Height - 40),
                    new Point(220, rect.Height - 60),
                    new Point(260, rect.Height - 45),
                    new Point(300, rect.Height - 55),
                    new Point(340, rect.Height - 35),
                };
                g.DrawLines(pen, points);

                using (var fillBrush = new SolidBrush(Color.FromArgb(52, 152, 219)))
                {
                    foreach (var pt in points)
                        g.FillEllipse(fillBrush, pt.X - 3, pt.Y - 3, 6, 6);
                }
            }
        }
    }

    public class NgClassificationPanel : UserControl
    {
        public NgClassificationPanel()
        {
            BackColor = Color.White;
            Margin = new Padding(4, 0, 4, 4);
            var lbl = new Label
            {
                Text = "NG分类统计",
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(12, 10),
                Size = new Size(200, 24)
            };

            var items = new[] {
                ("外观不良", 45, Color.FromArgb(231, 76, 60)),
                ("尺寸超差", 38, Color.FromArgb(230, 126, 34)),
                ("性能NG", 32, Color.FromArgb(241, 196, 15)),
                ("扫码异常", 25, Color.FromArgb(52, 152, 219)),
                ("设备异常", 20, Color.FromArgb(155, 89, 182)),
            };

            var pieArea = new Panel
            {
                Location = new Point(12, 38),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pieArea.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r = new Rectangle(10, 20, 120, 120);
                float total = 160f;
                float angle = 0f;
                foreach (var (_, count, color) in items)
                {
                    float sweep = count / total * 360f;
                    using (var brush = new SolidBrush(color))
                        g.FillPie(brush, r, angle, sweep);
                    angle += sweep;
                }
            };
            Controls.Add(lbl);
            Controls.Add(pieArea);

            int y = 42;
            foreach (var (name, count, color) in items)
            {
                var dot = new Panel
                {
                    Size = new Size(10, 10),
                    Location = new Point(150, y + 3),
                    BackColor = color
                };
                var text = new Label
                {
                    Text = $"{name}  {count}",
                    Location = new Point(166, y),
                    Size = new Size(120, 18),
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = Color.FromArgb(44, 62, 80)
                };
                dot.Paint += (s, e) => e.Graphics.FillEllipse(new SolidBrush(color), 0, 0, 10, 10);
                Controls.Add(dot);
                Controls.Add(text);
                y += 22;
            }
        }
    }

    public class StationStatusPanel : UserControl
    {
        public StationStatusPanel()
        {
            BackColor = Color.White;
            Margin = new Padding(4, 0, 0, 4);
            var lbl = new Label
            {
                Text = "工站状态",
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(12, 10),
                Size = new Size(200, 24)
            };
            Controls.Add(lbl);

            var stations = new[] {
                ("上料站", true), ("定位站", true), ("检测站", true),
                ("分拣站", false), ("下料站", true), ("扫码站", true)
            };

            int y = 42;
            foreach (var (name, ok) in stations)
            {
                var statusPanel = new Panel
                {
                    BackColor = ok ? Color.FromArgb(235, 249, 235) : Color.FromArgb(253, 237, 236),
                    Location = new Point(12, y),
                    Size = new Size(ClientSize.Width - 24, 28),
                    BorderStyle = BorderStyle.FixedSingle
                };
                var nameLabel = new Label
                {
                    Text = name,
                    Location = new Point(6, 4),
                    Font = new Font("Microsoft YaHei", 10f),
                    ForeColor = Color.FromArgb(44, 62, 80)
                };
                var statusLabel = new Label
                {
                    Text = ok ? "✓ 正常" : "✗ 异常",
                    Location = new Point(ClientSize.Width - 100, 4),
                    Size = new Size(80, 18),
                    Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                    ForeColor = ok ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60),
                    TextAlign = ContentAlignment.MiddleRight
                };
                statusPanel.Controls.Add(nameLabel);
                statusPanel.Controls.Add(statusLabel);
                Controls.Add(statusPanel);
                y += 34;
            }
        }
    }

    public class TrayVisionPanel : UserControl
    {
        public TrayVisionPanel()
        {
            BackColor = Color.White;
            Margin = new Padding(0, 4, 4, 4);
            var lbl = new Label
            {
                Text = "料仓状态 / 视觉监控",
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(12, 10),
                Size = new Size(300, 24)
            };
            Controls.Add(lbl);

            var trayGrid = new TableLayoutPanel
            {
                Location = new Point(12, 40),
                Size = new Size(500, ClientSize.Height - 52),
                ColumnCount = 4,
                RowCount = 3,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            for (int i = 0; i < 4; i++) trayGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int i = 0; i < 3; i++) trayGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            var states = new[] { "作业中", "待作业", "完成", "确认", "待确认", "空", "作业中", "待作业", "完成", "确认", "待确认", "空" };
            var colors = new[] {
                Color.FromArgb(52, 152, 219), Color.FromArgb(241, 196, 15), Color.FromArgb(39, 174, 96),
                Color.FromArgb(155, 89, 182), Color.FromArgb(149, 165, 166), Color.FromArgb(189, 195, 199),
            };
            for (int i = 0; i < 12; i++)
            {
                var cell = new Panel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = i < 6 ? colors[i % 6] : colors[i % 6],
                    Margin = new Padding(2)
                };
                var cellLabel = new Label
                {
                    Text = $"仓{i / 4 + 1}-盘{i % 4 + 1}\n{states[i]}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                    ForeColor = Color.White
                };
                cell.Controls.Add(cellLabel);
                trayGrid.Controls.Add(cell, i % 4, i / 4);
            }
            Controls.Add(trayGrid);
        }
    }

    public class MaterialPanel : UserControl
    {
        public MaterialPanel()
        {
            BackColor = Color.White;
            Margin = new Padding(4, 4, 0, 4);
            var lbl = new Label
            {
                Text = "辅料状态",
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(12, 10),
                Size = new Size(200, 24)
            };
            Controls.Add(lbl);

            var materials = new[] {
                ("UV胶", "充足", true), ("密封圈", "预警", false),
                ("清洗液", "充足", true), ("滤芯", "更换", false)
            };

            int y = 40;
            foreach (var (name, status, ok) in materials)
            {
                var row = new Panel
                {
                    Location = new Point(12, y),
                    Size = new Size(ClientSize.Width - 24, 30),
                    BackColor = ok ? Color.White : Color.FromArgb(255, 243, 205)
                };
                row.Controls.Add(new Label
                {
                    Text = name,
                    Location = new Point(4, 6),
                    Font = new Font("Microsoft YaHei", 10f),
                    ForeColor = Color.FromArgb(44, 62, 80)
                });
                row.Controls.Add(new Label
                {
                    Text = status,
                    Location = new Point(ClientSize.Width - 80, 6),
                    Size = new Size(60, 18),
                    Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                    ForeColor = ok ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60),
                    TextAlign = ContentAlignment.MiddleRight
                });
                Controls.Add(row);
                y += 34;
            }
        }
    }
}