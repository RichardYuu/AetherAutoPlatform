using System;
using System.Drawing;
using System.Windows.Forms;
using Aether.Platform.Core.Interfaces;

namespace Aether.Platform.UI.Modules
{
    public class VersionInfoView : UserControl, IModuleView
    {
        public string ModuleName => "VersionInfo";

        public VersionInfoView()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var mainPnl = new Panel
            {
                Size = new Size(620, 520),
                Location = new Point(30, 30),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14)
            };

            mainPnl.Controls.Add(new Label
            {
                Text = "\u7248\u672C\u4FE1\u606F",
                Location = new Point(14, 14),
                Size = new Size(580, 34),
                Font = new Font("Microsoft YaHei", 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 140),
                TextAlign = ContentAlignment.MiddleCenter
            });

            var line = new Panel { Location = new Point(14, 52), Size = new Size(580, 1), BackColor = Color.FromArgb(0, 120, 215) };
            mainPnl.Controls.Add(line);

            var info = new (string Key, string Value)[]
            {
                ("\u8F6F\u4EF6\u540D\u79F0", "Aether\u81EA\u52A8\u5316\u5E73\u53F0"),
                ("\u8F6F\u4EF6\u7248\u672C", "V1.0.0"),
                ("\u53D1\u5E03\u65E5\u671F", "2024-06-01"),
                ("\u8BBE\u5907\u540D\u79F0", "\u6CC4\u9732\u8BBE\u5907"),
                ("\u8BBE\u5907\u7F16\u53F7", "SYCZ.ZDX211.0001-0001.01"),
                ("\u8BBE\u5907\u578B\u53F7", "ZDX211"),
                ("\u5F00\u53D1\u73AF\u5883", ".NET Framework 4.7.2 / WinForms"),
                ("\u6570\u636E\u5E93", "SQL Server / Access (\u53EF\u914D\u7F6E)"),
                ("PLC\u7C7B\u578B", "\u6C47\u5DDD / \u57FA\u6069\u58EB (\u53EF\u914D\u7F6E)"),
            };

            int y = 64;
            foreach (var item in info)
            {
                var row = new Panel { Location = new Point(14, y), Size = new Size(580, 32), BackColor = Color.Transparent };
                row.Controls.Add(new Label { Text = item.Key + "\uFF1A", Location = new Point(0, 4), Size = new Size(150, 24), Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(60, 60, 60) });
                row.Controls.Add(new Label { Text = item.Value, Location = new Point(152, 4), Size = new Size(420, 24), Font = new Font("Microsoft YaHei", 10f), ForeColor = Color.FromArgb(0, 80, 160) });
                mainPnl.Controls.Add(row);
                y += 38;
            }

            y += 12;
            mainPnl.Controls.Add(new Label { Text = "\u57C3\u68EE\u79D1\u6280\u6709\u9650\u516C\u53F8", Location = new Point(14, y), Size = new Size(580, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 10f), ForeColor = Color.FromArgb(120, 120, 120) });

            y += 30;
            mainPnl.Controls.Add(new Label { Text = "Copyright \u00A9 2024 AETHER TECHNOLOGIES CO., LTD. All Rights Reserved.", Location = new Point(14, y), Size = new Size(580, 26), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.FromArgb(150, 150, 150) });

            Controls.Add(mainPnl);
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData() { }
    }
}