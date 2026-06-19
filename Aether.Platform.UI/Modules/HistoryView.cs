using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;

namespace Aether.Platform.UI.Modules
{
    public class HistoryView : UserControl, IModuleView
    {
        public string ModuleName => "History";
        private Panel _tablePanel;
        private Label _pageInfo;
        private TextBox _searchBox;
        private ComboBox _processFilter;
        private DateTimePicker _dtpFrom, _dtpTo;
        private Label _summaryLabel;
        private int _currentPage = 1;
        private const int PageSize = 12;
        private List<HistoryRecord> _records;
        private List<HistoryRecord> _filtered;

        private static readonly Color HeaderBg = Color.FromArgb(230, 235, 240);
        private static readonly Color OkGreen = Color.FromArgb(39, 174, 96);
        private static readonly Color NgRed = Color.FromArgb(231, 76, 60);
        private static readonly Color RowAltBg = Color.FromArgb(248, 249, 252);

        public HistoryView()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Padding = new Padding(4);
            _records = new List<HistoryRecord>();
            _filtered = new List<HistoryRecord>();
            BuildLayout();
            Task.Run(async () => await LoadDataAsync());
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var db = AppBootstrap.Instance?.DbContext?.Provider;
                if (db != null && db.IsAvailable)
                {
                    var sql = "SELECT TOP 200 TestTime AS DateTime, SerialNumber AS ProductCode, "
                            + "ProcessName AS Process, StationId AS Station, Result, "
                            + "ISNULL(CAST(TestValue AS NVARCHAR), '-') AS Value, "
                            + "ISNULL(ValueUnit, '-') AS ValueUnit, "
                            + "OperatorName AS Operator, WorkOrder "
                            + "FROM ProductionRecords WHERE TestTime >= @From AND TestTime <= @To "
                            + "ORDER BY TestTime DESC";

                    var from = _dtpFrom?.Value.Date ?? DateTime.Now.AddDays(-7);
                    var to = (_dtpTo?.Value.Date ?? DateTime.Now).AddDays(1).AddSeconds(-1);

                    var result = await db.QueryAsync<HistoryRecord>(sql, new { From = from, To = to });
                    var list = result?.ToList();
                    if (list != null && list.Count > 0)
                    {
                        _records = list;
                    }
                    else
                    {
                        LoadSampleData();
                    }
                }
                else
                {
                    LoadSampleData();
                }
            }
            catch
            {
                LoadSampleData();
            }

            _filtered = new List<HistoryRecord>(_records);
            BeginInvoke(new Action(() => RenderTable()));
        }

        private void LoadSampleData()
        {
            if (_records.Count > 0) return; // 已有数据则不覆盖

            var rand = new Random(42);
            var stations = new[] { "工站1", "工站2", "工站3", "工站1", "工站2", "工站3" };
            var ops = new[] { "张三", "李四", "王五", "赵六" };
            var processes = new[] { "MTF测试", "泄露测试", "偏心测试", "点胶", "杂光检测", "锁付", "组装" };

            _records = new List<HistoryRecord>();
            for (int i = 0; i < 60; i++)
            {
                _records.Add(new HistoryRecord
                {
                    DateTime = DateTime.Now.AddMinutes(-i * 3).AddSeconds(rand.Next(60)),
                    ProductCode = $"P{DateTime.Now.AddDays(-i / 20):yyyyMMdd}{i + 1:D4}",
                    Process = processes[rand.Next(processes.Length)],
                    Station = stations[rand.Next(stations.Length)],
                    Result = rand.Next(10) < 8 ? "OK" : "NG",
                    Value = Math.Round(rand.NextDouble() * 0.1, 3).ToString("F3"),
                    ValueUnit = "Pa",
                    Operator = ops[rand.Next(ops.Length)],
                    WorkOrder = $"WO{20240501 + rand.Next(30):D8}",
                });
            }
        }

        private void BuildLayout()
        {
            // 筛选栏
            var filterBar = new Panel { Height = 46, Dock = DockStyle.Top, BackColor = Color.FromArgb(235, 238, 245), Padding = new Padding(8, 6, 8, 6) };

            int fx = 8;
            AddFilterLabel(filterBar, "时间范围:", ref fx);
            _dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(fx, 8), Size = new Size(100, 22), Value = DateTime.Now.AddDays(-7) };
            filterBar.Controls.Add(_dtpFrom); fx += 108;
            AddFilterLabel(filterBar, "至", ref fx);
            _dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(fx, 8), Size = new Size(100, 22) };
            filterBar.Controls.Add(_dtpTo); fx += 120;

            AddFilterLabel(filterBar, "工序:", ref fx);
            _processFilter = new ComboBox { Location = new Point(fx, 8), Size = new Size(100, 22), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Microsoft YaHei", 8.5f) };
            _processFilter.Items.AddRange(new[] { "全部", "MTF测试", "泄露测试", "偏心测试", "点胶", "杂光检测", "锁付", "组装" });
            _processFilter.SelectedIndex = 0; fx += 108;

            AddFilterLabel(filterBar, "搜索:", ref fx);
            _searchBox = new TextBox { Location = new Point(fx, 8), Size = new Size(120, 22), Font = new Font("Microsoft YaHei", 8.5f) };
            filterBar.Controls.Add(_searchBox); fx += 130;

            var btnQuery = new Button { Text = "查询", Location = new Point(fx, 6), Size = new Size(60, 28), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnQuery.FlatAppearance.BorderSize = 0; btnQuery.Click += async (s, e) => { await LoadDataAsync(); };
            filterBar.Controls.Add(btnQuery);

            var btnExport = new Button { Text = "导出CSV", Location = new Point(fx + 66, 6), Size = new Size(70, 28), BackColor = Color.FromArgb(76, 175, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ExportCsv();
            filterBar.Controls.Add(btnExport);

            var btnClear = new Button { Text = "清除", Location = new Point(fx + 142, 6), Size = new Size(50, 28), BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnClear.FlatAppearance.BorderSize = 0; btnClear.Click += async (s, e) => { _searchBox.Text = ""; _processFilter.SelectedIndex = 0; await LoadDataAsync(); };
            filterBar.Controls.Add(btnClear);
            Controls.Add(filterBar);

            // 统计摘要
            var summaryBar = new Panel { Height = 28, Dock = DockStyle.Top, BackColor = Color.FromArgb(255, 248, 220), Padding = new Padding(8, 4, 8, 4) };
            _summaryLabel = new Label { Text = "正在加载数据...", Location = new Point(8, 4), Size = new Size(500, 20), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.FromArgb(120, 100, 20) };
            summaryBar.Controls.Add(_summaryLabel);
            Controls.Add(summaryBar);

            // 表格区
            _tablePanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
            Controls.Add(_tablePanel);

            // 分页栏
            var pageBar = new Panel { Height = 38, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(240, 242, 246), Padding = new Padding(4) };
            var btnPrev = new Button { Text = "< 上一页", Location = new Point(8, 6), Size = new Size(80, 26), BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnPrev.FlatAppearance.BorderSize = 0; btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; RenderTable(); } };
            pageBar.Controls.Add(btnPrev);

            var btnNext = new Button { Text = "下一页 >", Location = new Point(94, 6), Size = new Size(80, 26), BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnNext.FlatAppearance.BorderSize = 0; btnNext.Click += (s, e) => { if (_currentPage < TotalPages) { _currentPage++; RenderTable(); } };
            pageBar.Controls.Add(btnNext);

            _pageInfo = new Label { Text = "", Location = new Point(184, 8), Size = new Size(300, 20), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.FromArgb(100, 100, 100) };
            pageBar.Controls.Add(_pageInfo);
            Controls.Add(pageBar);

            RenderTable();
        }

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));

        private void AddFilterLabel(Panel p, string t, ref int x)
        {
            p.Controls.Add(new Label { Text = t, Location = new Point(x, 10), Size = new Size(TextRenderer.MeasureText(t, new Font("Microsoft YaHei", 8.5f)).Width + 4, 20), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.FromArgb(80, 80, 80) });
            x += TextRenderer.MeasureText(t, new Font("Microsoft YaHei", 8.5f)).Width + 6;
        }

        private void ApplyFilter()
        {
            _filtered = new List<HistoryRecord>(_records);

            var proc = _processFilter.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(proc) && proc != "全部")
                _filtered = _filtered.Where(r => r.Process == proc).ToList();

            var search = _searchBox.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
                _filtered = _filtered.Where(r => r.ProductCode.Contains(search) || r.WorkOrder.Contains(search)).ToList();

            _currentPage = 1;
            RenderTable();
        }

        private void RenderTable()
        {
            _tablePanel.Controls.Clear();
            var headers = new[] { "时间", "产品码", "工序", "工站", "结果", "数值", "值单位", "操作员", "工单" };
            var widths = new[] { 150, 135, 85, 70, 55, 75, 60, 70, 110 };

            // 表头
            int x = 4, hy = 2;
            for (int i = 0; i < headers.Length; i++)
            {
                _tablePanel.Controls.Add(new Label
                {
                    Text = headers[i], Location = new Point(x, hy), Size = new Size(widths[i], 30),
                    BackColor = HeaderBg, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                });
                x += widths[i] + 2;
            }

            // 数据行
            int y = 34;
            var pageItems = _filtered.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            foreach (var rec in pageItems)
            {
                x = 4;
                var vals = new[] { rec.DateTime.ToString("yyyy/MM/dd HH:mm:ss"), rec.ProductCode, rec.Process, rec.Station, rec.Result, rec.Value, rec.ValueUnit, rec.Operator, rec.WorkOrder };

                for (int i = 0; i < vals.Length; i++)
                {
                    Color fg = Color.Black;
                    if (i == 4) fg = vals[i] == "NG" ? NgRed : OkGreen;

                    _tablePanel.Controls.Add(new Label
                    {
                        Text = vals[i], Location = new Point(x, y), Size = new Size(widths[i], 26),
                        Font = new Font("Microsoft YaHei", 8.5f), ForeColor = fg,
                        TextAlign = ContentAlignment.MiddleCenter
                    });
                    x += widths[i] + 2;
                }
                y += 28;
            }

            _pageInfo.Text = $"共 {_filtered.Count} 条记录  第 {_currentPage}/{TotalPages} 页";

            // 更新统计摘要
            var okCount = _records.Count(r => r.Result == "OK");
            var ngCount = _records.Count(r => r.Result == "NG");
            if (_summaryLabel != null)
            {
                _summaryLabel.Text = _records.Count > 0
                    ? $"总计 {_records.Count} 条 | 合格 {okCount} | 不合格 {ngCount} | 良率 {(okCount * 100.0 / _records.Count):F1}%"
                    : "暂无生产记录";
            }
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData()
        {
            Task.Run(async () => await LoadDataAsync());
        }

        private void ExportCsv()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV 文件|*.csv",
                FileName = $"生产记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "导出生产记录"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("时间,产品码,工序,工站,结果,数值,值单位,操作员,工单");
                    foreach (var r in _records)
                    {
                        sb.AppendLine($"\"{r.DateTime:yyyy/MM/dd HH:mm:ss}\",\"{r.ProductCode}\",\"{r.Process}\",\"{r.Station}\",\"{r.Result}\",\"{r.Value}\",\"{r.ValueUnit}\",\"{r.Operator}\",\"{r.WorkOrder}\"");
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"成功导出 {_records.Count} 条记录", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    public class HistoryRecord
    {
        public DateTime DateTime { get; set; }
        public string ProductCode { get; set; }
        public string Process { get; set; }
        public string Station { get; set; }
        public string Result { get; set; }
        public string Value { get; set; }
        public string ValueUnit { get; set; }
        public string Operator { get; set; }
        public string WorkOrder { get; set; }
    }
}