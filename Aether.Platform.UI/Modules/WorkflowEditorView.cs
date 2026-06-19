using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Workflow;

namespace Aether.Platform.UI.Modules
{
    public class WorkflowEditorView : UserControl, IModuleView
    {
        public string ModuleName => "WorkflowEditor";
        private WorkflowCanvas _canvas;
        private Panel _propPanel;
        private Label _lblNodeName;
        private TextBox _txtNodeName;
        private ComboBox _cmbNodeType;
        private TextBox _txtScript;
        private TextBox _txtDelay;
        private WorkflowNode _selectedNode;
        private Panel _dynamicPropsPanel;
        private int _propsBaseY;

        private static readonly Color AccentBlue = Color.FromArgb(0, 120, 215);
        private static readonly Color PanelBg = Color.FromArgb(245, 246, 248);

        public WorkflowEditorView()
        {
            this.BackColor = PanelBg;
            BuildLayout();
        }

        private void BuildLayout()
        {
            // 工具栏
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(230, 235, 242), Padding = new Padding(8, 4, 8, 4) };

            int tx = 8;
            var btnNew = new Button { Text = "新建", Location = new Point(tx, 4), Size = new Size(58, 30), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnNew.FlatAppearance.BorderSize = 0; btnNew.Click += (s, e) => NewWorkflow(); toolbar.Controls.Add(btnNew); tx += 64;

            var btnLoad = new Button { Text = "加载", Location = new Point(tx, 4), Size = new Size(58, 30), BackColor = Color.FromArgb(50, 140, 220), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnLoad.FlatAppearance.BorderSize = 0; btnLoad.Click += (s, e) => LoadWorkflow(); toolbar.Controls.Add(btnLoad); tx += 64;

            var btnSave = new Button { Text = "保存", Location = new Point(tx, 4), Size = new Size(58, 30), BackColor = Color.FromArgb(50, 140, 220), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnSave.FlatAppearance.BorderSize = 0; btnSave.Click += (s, e) => SaveWorkflow(); toolbar.Controls.Add(btnSave); tx += 76;

            AddToolBtn(toolbar, "添加起始", ref tx, Color.FromArgb(39, 174, 96), WorkflowNodeType.Start);
            AddToolBtn(toolbar, "添加结束", ref tx, Color.FromArgb(231, 76, 60), WorkflowNodeType.End);
            AddToolBtn(toolbar, "添加延时", ref tx, Color.FromArgb(243, 156, 18), WorkflowNodeType.Delay);
            AddToolBtn(toolbar, "添加轴动", ref tx, Color.FromArgb(155, 89, 182), WorkflowNodeType.AxisMove);
            tx += 12;

            var btnRun = new Button { Text = "▶ 运行", Location = new Point(tx, 4), Size = new Size(68, 30), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnRun.FlatAppearance.BorderSize = 0; btnRun.Click += (s, e) => RunWorkflow(); toolbar.Controls.Add(btnRun); tx += 74;

            var btnStop = new Button { Text = "■ 停止", Location = new Point(tx, 4), Size = new Size(68, 30), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnStop.FlatAppearance.BorderSize = 0; btnStop.Click += (s, e) => StopWorkflow(); toolbar.Controls.Add(btnStop); tx += 74;

            var btnArrange = new Button { Text = "自动排列", Location = new Point(tx, 4), Size = new Size(68, 30), BackColor = Color.FromArgb(120, 130, 150), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnArrange.FlatAppearance.BorderSize = 0; btnArrange.Click += (s, e) => _canvas.AutoArrange(); toolbar.Controls.Add(btnArrange);
            tx += 72;

            var btnResetView = new Button { Text = "重置视角", Location = new Point(tx, 4), Size = new Size(68, 30), BackColor = Color.FromArgb(100, 110, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnResetView.FlatAppearance.BorderSize = 0; btnResetView.Click += (s, e) => _canvas.ResetView(); toolbar.Controls.Add(btnResetView);

            Controls.Add(toolbar);

            // 主区域：Canvas (左) + 属性面板 (右)
            var mainArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            _canvas = new WorkflowCanvas { Dock = DockStyle.Fill };
            _canvas.OnNodeSelected += OnCanvasNodeSelected;
            _canvas.OnNodeDoubleClicked += OnCanvasNodeDoubleClicked;
            _canvas.OnConnectionCreated += conn => { };
            mainArea.Controls.Add(_canvas);

            // 右侧属性面板
            _propPanel = new Panel { Dock = DockStyle.Right, Width = 240, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(8) };
            BuildPropertyPanel();
            mainArea.Controls.Add(_propPanel);

            Controls.Add(mainArea);

            // 节点类型调色板 (底部)
            var palette = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.FromArgb(238, 240, 245), Padding = new Padding(4) };
            palette.Controls.Add(new Label { Text = "节点: ", Location = new Point(8, 12), Size = new Size(45, 20), Font = new Font("Microsoft YaHei", 9f), ForeColor = Color.FromArgb(80, 80, 80) });
            int px = 54;
            foreach (var (name, color, type) in new (string, Color, WorkflowNodeType)[]
            {
                ("开始", Color.FromArgb(39,174,96), WorkflowNodeType.Start),
                ("结束", Color.FromArgb(231,76,60), WorkflowNodeType.End),
                ("延时", Color.FromArgb(243,156,18), WorkflowNodeType.Delay),
                ("轴移动", Color.FromArgb(155,89,182), WorkflowNodeType.AxisMove),
                ("IO写", Color.FromArgb(52,152,219), WorkflowNodeType.DioWrite),
                ("PLC写", Color.FromArgb(52,152,219), WorkflowNodeType.PlcWrite),
                ("视觉", Color.FromArgb(22,160,133), WorkflowNodeType.VisionCapture),
                ("扫码", Color.FromArgb(22,160,133), WorkflowNodeType.ScannerRead),
                ("Lua", Color.FromArgb(142,68,173), WorkflowNodeType.LuaScript),
                ("日志", Color.FromArgb(125,130,140), WorkflowNodeType.Log),
                ("循环", Color.FromArgb(211,84,0), WorkflowNodeType.Loop),
                ("条件", Color.FromArgb(41,128,185), WorkflowNodeType.IfCondition),
            })
            {
                var chip = new Label
                {
                    Text = name, Location = new Point(px, 7), Size = new Size(52, 26),
                    BackColor = color, ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold),
                    Cursor = Cursors.Hand, Tag = type
                };
                chip.Click += (s, e) =>
                {
                    if (s is Label l && l.Tag is WorkflowNodeType nodeType)
                        AddNodeToCanvas(nodeType);
                };
                palette.Controls.Add(chip);
                px += 58;
            }
            Controls.Add(palette);

            // 加载默认示例工作流
            LoadDemoWorkflow();
        }

        private void BuildPropertyPanel()
        {
            var title = new Label { Text = "节点属性", Dock = DockStyle.Top, Height = 30, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = AccentBlue, TextAlign = ContentAlignment.MiddleLeft };
            _propPanel.Controls.Add(title);

            int y = 36;

            _propPanel.Controls.Add(new Label { Text = "ID", Location = new Point(6, y), Size = new Size(30, 20), Font = new Font("Microsoft YaHei", 8.5f), ForeColor = Color.Gray });
            _lblNodeName = new Label { Text = "(未选择)", Location = new Point(38, y), Size = new Size(180, 20), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
            _propPanel.Controls.Add(_lblNodeName); y += 28;

            _propPanel.Controls.Add(new Label { Text = "名称", Location = new Point(6, y), Size = new Size(30, 20), Font = new Font("Microsoft YaHei", 8.5f) });
            _txtNodeName = new TextBox { Location = new Point(38, y), Size = new Size(180, 22), Font = new Font("Microsoft YaHei", 8.5f) };
            _txtNodeName.TextChanged += (s, e) => { if (_selectedNode != null) _selectedNode.Name = _txtNodeName.Text; };
            _propPanel.Controls.Add(_txtNodeName); y += 30;

            _propPanel.Controls.Add(new Label { Text = "类型", Location = new Point(6, y), Size = new Size(30, 20), Font = new Font("Microsoft YaHei", 8.5f) });
            _cmbNodeType = new ComboBox { Location = new Point(38, y), Size = new Size(180, 22), Font = new Font("Microsoft YaHei", 8.5f), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var t in Enum.GetNames(typeof(WorkflowNodeType)))
                _cmbNodeType.Items.Add(t);
            _propPanel.Controls.Add(_cmbNodeType); y += 32;

            // 脚本编辑 (仅对 Lua 节点可见)
            _propPanel.Controls.Add(new Label { Text = "Lua脚本", Location = new Point(6, y), Size = new Size(50, 20), Font = new Font("Microsoft YaHei", 8.5f) });
            _txtScript = new TextBox { Location = new Point(6, y + 24), Size = new Size(216, 120), Multiline = true, Font = new Font("Consolas", 8.5f), Visible = false, ScrollBars = ScrollBars.Vertical };
            _propPanel.Controls.Add(_txtScript);

            // 延时 (仅对 Delay 节点可见)
            _propPanel.Controls.Add(new Label { Text = "延时(ms)", Location = new Point(6, y + 140), Size = new Size(50, 20), Font = new Font("Microsoft YaHei", 8.5f) });
            _txtDelay = new TextBox { Location = new Point(64, y + 138), Size = new Size(80, 22), Font = new Font("Microsoft YaHei", 8.5f), Visible = false };
            _propPanel.Controls.Add(_txtDelay);

            // 删除按钮
            var btnDelete = new Button { Text = "删除节点", Location = new Point(6, y + 170), Size = new Size(80, 28), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) => DeleteSelectedNode();
            _propPanel.Controls.Add(btnDelete);

            // 动态属性区
            _propsBaseY = y + 210;
            _dynamicPropsPanel = new Panel
            {
                Location = new Point(4, _propsBaseY),
                Size = new Size(216, 200),
                AutoScroll = true,
                BackColor = Color.FromArgb(250, 251, 253),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            _propPanel.Controls.Add(_dynamicPropsPanel);
        }

        private void AddToolBtn(Panel p, string t, ref int x, Color c, WorkflowNodeType? nodeType = null)
        {
            var b = new Button { Text = t, Location = new Point(x, 4), Size = new Size(t.Length > 3 ? 68 : 58, 30), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
            b.FlatAppearance.BorderSize = 0;
            if (nodeType.HasValue)
            {
                var nt = nodeType.Value;
                b.Click += (s, e) => AddNodeToCanvas(nt);
            }
            p.Controls.Add(b); x += b.Width + 4;
        }

        private void OnCanvasNodeSelected(WorkflowNode node)
        {
            _selectedNode = node;
            _dynamicPropsPanel.Controls.Clear();
            _dynamicPropsPanel.Visible = false;

            if (node == null)
            {
                _lblNodeName.Text = "(未选择)";
                _txtNodeName.Text = "";
                _txtScript.Visible = false;
                _txtDelay.Visible = false;
                return;
            }

            _lblNodeName.Text = $"ID: {node.Id}";
            _txtNodeName.Text = node.Name;
            _cmbNodeType.SelectedItem = node.Type.ToString();

            _txtScript.Visible = node.Type == WorkflowNodeType.LuaScript;
            _txtDelay.Visible = node.Type == WorkflowNodeType.Delay;

            if (node.Type == WorkflowNodeType.LuaScript)
                _txtScript.Text = node.GetProperty("Script", "");
            if (node.Type == WorkflowNodeType.Delay)
                _txtDelay.Text = node.GetProperty("Milliseconds", 1000).ToString();

            // 构建动态属性面板
            BuildDynamicProps(node);
        }

        private void BuildDynamicProps(WorkflowNode node)
        {
            if (node.Properties == null || node.Properties.Count == 0) return;

            var excludedKeys = new HashSet<string> { "Script", "Milliseconds" };

            var entries = node.Properties.Where(kv => !excludedKeys.Contains(kv.Key)).ToList();
            if (entries.Count == 0) return;

            _dynamicPropsPanel.Visible = true;
            int dy = 4;

            var headerLbl = new Label
            {
                Text = "自定义属性",
                Location = new Point(4, dy),
                Size = new Size(200, 18),
                Font = new Font("Microsoft YaHei", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 120)
            };
            _dynamicPropsPanel.Controls.Add(headerLbl);
            dy += 22;

            foreach (var kv in entries)
            {
                var keyLbl = new Label
                {
                    Text = kv.Key,
                    Location = new Point(6, dy),
                    Size = new Size(70, 22),
                    Font = new Font("Microsoft YaHei", 8f),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    TextAlign = ContentAlignment.MiddleRight
                };
                _dynamicPropsPanel.Controls.Add(keyLbl);

                var valBox = new TextBox
                {
                    Text = kv.Value?.ToString() ?? "",
                    Location = new Point(80, dy),
                    Size = new Size(128, 22),
                    Font = new Font("Consolas", 8.5f),
                    BackColor = Color.FromArgb(255, 255, 225),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = kv.Key
                };
                valBox.TextChanged += (s, e) =>
                {
                    if (_selectedNode != null && s is TextBox tb && tb.Tag is string propKey)
                    {
                        _selectedNode.Properties[propKey] = tb.Text;
                    }
                };
                _dynamicPropsPanel.Controls.Add(valBox);
                dy += 26;
            }
        }

        private void OnCanvasNodeDoubleClicked(WorkflowNode node)
        {
            if (node.Type == WorkflowNodeType.LuaScript)
            {
                _txtScript.Focus();
            }
        }

        private void AddNodeToCanvas(WorkflowNodeType type)
        {
            var def = _canvas.Definition;
            if (def == null)
            {
                def = new WorkflowDefinition { Id = "WF_New", Name = "新工作流" };
                _canvas.LoadWorkflow(def);
            }

            var node = new WorkflowNode
            {
                Id = $"N{def.Nodes.Count + 1}",
                Name = type.ToString(),
                Type = type,
                Position = new Point(20 + def.Nodes.Count * 10, 20 + def.Nodes.Count * 10),
                Size = new Size(120, 50)
            };

            if (type == WorkflowNodeType.Delay)
                node.Properties["Milliseconds"] = 1000;
            if (type == WorkflowNodeType.LuaScript)
                node.Properties["Script"] = "print('hello')";
            if (type == WorkflowNodeType.AxisMove)
            {
                node.Properties["AxisId"] = "Z";
                node.Properties["Target"] = 100.0;
                node.Properties["Speed"] = 50.0;
            }
            if (type == WorkflowNodeType.DioWrite)
            {
                node.Properties["Index"] = 0;
                node.Properties["Value"] = true;
            }
            if (type == WorkflowNodeType.PlcWrite)
            {
                node.Properties["Address"] = "D0";
                node.Properties["Value"] = 0;
            }
            if (type == WorkflowNodeType.VisionCapture)
            {
                node.Properties["VisionId"] = "CAM-01";
                node.Properties["Recipe"] = "DefaultRecipe";
            }
            if (type == WorkflowNodeType.ScannerRead)
            {
                node.Properties["ScannerId"] = "SC-01";
                node.Properties["Timeout"] = 3000;
            }
            if (type == WorkflowNodeType.IfCondition)
            {
                node.Properties["Expression"] = "x > 0";
            }
            if (type == WorkflowNodeType.Loop)
            {
                node.Properties["Count"] = 3;
            }
            if (type == WorkflowNodeType.Log)
            {
                node.Properties["Level"] = "Info";
                node.Properties["Message"] = "";
            }

            def.Nodes.Add(node);
            _canvas.Invalidate();
        }

        private void DeleteSelectedNode()
        {
            if (_selectedNode == null) return;
            var def = _canvas.Definition;
            if (def == null) return;

            def.Connections.RemoveAll(c => c.FromNodeId == _selectedNode.Id || c.ToNodeId == _selectedNode.Id);
            def.Nodes.Remove(_selectedNode);
            _selectedNode = null;
            OnCanvasNodeSelected(null);
            _canvas.Invalidate();
        }

        private void LoadDemoWorkflow()
        {
            var builder = new WorkflowBuilder("WF_Demo", "示例工作流", "Demo");
            builder.Start("开始")
                   .ScannerRead()
                   .AxisMove("Z", 0, 30)
                   .Delay(2000)
                   .VisionCapture("default")
                   .LuaScript("print('检测完成')\nmsleep(500)")
                   .DioWrite(0, true)
                   .Delay(1000)
                   .Log("流程结束")
                   .End("结束");

            var def = builder.Build();
            _canvas.LoadWorkflow(def);
            _canvas.AutoArrange();
        }

        // ===== 新建/保存/加载 =====

        private void NewWorkflow()
        {
            var def = new WorkflowDefinition { Id = "WF_New", Name = "新工作流" };
            _canvas.LoadWorkflow(def);
            _selectedNode = null;
            OnCanvasNodeSelected(null);
            AppBootstrap.Instance?.OnSystemLog?.Invoke("Workflow", "新建工作流");
        }

        private void SaveWorkflow()
        {
            var def = _canvas.Definition;
            if (def == null)
            {
                MessageBox.Show("没有可保存的工作流", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "JSON 工作流|*.json",
                FileName = $"{def.Id ?? "workflow"}.json",
                DefaultExt = ".json"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var json = JsonConvert.SerializeObject(def, Formatting.Indented);
                    File.WriteAllText(sfd.FileName, json);
                    def.FilePath = sfd.FileName;
                    AppBootstrap.Instance?.OnSystemLog?.Invoke("Workflow", $"保存工作流: {sfd.FileName}");
                }
            }
        }

        private void LoadWorkflow()
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "JSON 工作流|*.json",
                DefaultExt = ".json"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var json = File.ReadAllText(ofd.FileName);
                    var def = JsonConvert.DeserializeObject<WorkflowDefinition>(json);
                    if (def == null)
                    {
                        MessageBox.Show("文件格式无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    def.FilePath = ofd.FileName;
                    _canvas.LoadWorkflow(def);
                    _canvas.AutoArrange();
                    _selectedNode = null;
                    OnCanvasNodeSelected(null);
                    AppBootstrap.Instance?.OnSystemLog?.Invoke("Workflow", $"加载工作流: {ofd.FileName}");
                }
            }
        }

        // ===== 运行/停止 =====

        private void RunWorkflow()
        {
            var def = _canvas.Definition;
            if (def == null)
            {
                MessageBox.Show("请先创建或加载工作流", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var bootstrap = AppBootstrap.Instance;
            if (bootstrap == null)
            {
                MessageBox.Show("AppBootstrap 未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var hooks = new WorkflowExecutionHooks
            {
                LogCallback = (category, message) =>
                {
                    bootstrap.OnSystemLog?.Invoke($"WF/{category}", message);
                }
            };

            var engine = new WorkflowEngine(def, hooks);
            bootstrap.ActiveWorkflow = engine;
            bootstrap.OnSystemLog?.Invoke("Workflow", $"启动工作流: {def.Name}");

            try
            {
                // 异步启动工作流（不阻塞 UI 线程）
                System.Threading.Tasks.Task.Run(async () =>
                {
                    var result = await engine.RunAsync();
                    BeginInvoke((Action)(() =>
                    {
                        bootstrap.OnSystemLog?.Invoke("Workflow", $"工作流结束: {result}");
                    }));
                });
            }
            catch (Exception ex)
            {
                bootstrap.OnSystemLog?.Invoke("Workflow", $"运行失败: {ex.Message}");
                MessageBox.Show($"运行失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopWorkflow()
        {
            var bootstrap = AppBootstrap.Instance;
            var engine = bootstrap?.ActiveWorkflow;
            if (engine == null) return;

            engine.Abort();
            bootstrap.ActiveWorkflow = null;
            AppBootstrap.Instance?.OnSystemLog?.Invoke("Workflow", "停止工作流");
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData() { }
    }
}