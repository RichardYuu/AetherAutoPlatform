using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Data;
using Aether.Platform.Data.Configuration;

namespace Aether.Platform.UI.Modules
{
    public class ProcessDebugView : UserControl, IModuleView
    {
        public string ModuleName => "ProcessDebug";
        private Panel _paramPanel;
        private Label _currentNodeLabel;
        private Label _hintBar;
        private Dictionary<string, TextBox> _editableFields = new Dictionary<string, TextBox>();
        private string _currentSection;

        private static readonly Color HeaderBg = Color.FromArgb(230, 235, 240);
        private static readonly Color WhiteBg = Color.White;
        private static readonly Color RowAltBg = Color.FromArgb(248, 249, 252);
        private static readonly Color OkGreen = Color.FromArgb(76, 175, 80);

        public ProcessDebugView()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            BuildLayout();
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.Controls.Add(BuildTreePanel(), 0, 0);
            table.Controls.Add(BuildParamDetailPanel(), 1, 0);
            Controls.Add(table);
        }

        private Panel BuildTreePanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = WhiteBg, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(2), Margin = new Padding(3) };
            pnl.Controls.Add(new Label { Text = "参数导航", Dock = DockStyle.Top, Height = 32, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 70, 140), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(235, 240, 248) });

            var tree = new TreeView { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 251, 253), Font = new Font("Microsoft YaHei", 9f), BorderStyle = BorderStyle.None, Indent = 20 };

            // 设备参数
            var devNode = tree.Nodes.Add("设备参数");
            devNode.Nodes.Add("功能配置");
            devNode.Nodes.Add("轴速度");
            devNode.Nodes.Add("IO映射");
            devNode.Nodes.Add("通讯设置");

            // 工艺参数 — 按工序分类
            var procNode = tree.Nodes.Add("工艺参数");
            procNode.Nodes.Add("泄露测试");
            procNode.Nodes.Add("点胶控制");
            procNode.Nodes.Add("锁付控制");
            procNode.Nodes.Add("视觉定位");
            procNode.Nodes.Add("激光打标");
            procNode.Nodes.Add("干冰清洗");
            procNode.Nodes.Add("组装压合");

            // 信息化
            var infoNode = tree.Nodes.Add("信息化参数");
            infoNode.Nodes.Add("MES上传");
            infoNode.Nodes.Add("IFMS配置");
            infoNode.Nodes.Add("数据追溯");
            infoNode.Nodes.Add("条码规则");

            // 高级
            var advNode = tree.Nodes.Add("高级");
            advNode.Nodes.Add("算法参数");
            advNode.Nodes.Add("标定参数");
            advNode.Nodes.Add("诊断日志");

            tree.ExpandAll();
            tree.AfterSelect += (s, e) =>
            {
                string path = e.Node?.FullPath ?? "";
                ShowParams(path);
            };

            // 默认选中第一个
            if (tree.Nodes[0].Nodes.Count > 0)
                tree.SelectedNode = tree.Nodes[0].Nodes[0];

            pnl.Controls.Add(tree);
            return pnl;
        }

        private Panel BuildParamDetailPanel()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = WhiteBg, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(3) };

            var header = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(240, 243, 248) };
            _currentNodeLabel = new Label { Text = "功能配置", Location = new Point(6, 6), Size = new Size(200, 26), BackColor = OkGreen, ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold) };
            header.Controls.Add(_currentNodeLabel);

            var btnBar = new Panel { Dock = DockStyle.Right, Width = 260, BackColor = Color.Transparent };
            foreach (var (t, bx) in new[] { ("应用修改", 0), ("批量导入", 90), ("批量导出", 180) })
            {
                var b = new Button { Text = t, Location = new Point(bx, 5), Size = new Size(80, 26), BackColor = Color.FromArgb(70, 140, 210), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8.5f) };
                b.FlatAppearance.BorderSize = 0;

                if (t == "应用修改") b.Click += (s, e) => ApplyParams();
                else if (t == "批量导入") b.Click += (s, e) => ImportParams();
                else b.Click += (s, e) => ExportParams();

                btnBar.Controls.Add(b);
            }
            header.Controls.Add(btnBar);
            pnl.Controls.Add(header);

            _hintBar = new Label { Text = "", Dock = DockStyle.Top, Height = 22, BackColor = Color.FromArgb(255, 248, 220), Font = new Font("Microsoft YaHei", 8f), ForeColor = Color.FromArgb(180, 120, 30), Padding = new Padding(6, 2, 0, 0) };
            pnl.Controls.Add(_hintBar);

            _paramPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
            pnl.Controls.Add(_paramPanel);

            return pnl;
        }

        private void ShowParams(string nodePath)
        {
            _paramPanel?.Controls.Clear();
            _editableFields.Clear();
            _currentSection = nodePath;
            _currentNodeLabel.Text = nodePath.Replace("\\", " → ");
            _hintBar.Text = "";

            switch (nodePath)
            {
                case "设备参数\\功能配置":  ShowFunctionParams(); break;
                case "设备参数\\轴速度":    ShowAxisSpeedParams(); break;
                case "设备参数\\IO映射":    ShowIOMappingParams(); break;
                case "设备参数\\通讯设置":  ShowCommParams(); break;
                case "工艺参数\\泄露测试":  ShowLeakTestParams(); break;
                case "工艺参数\\点胶控制":  ShowDispenseParams(); break;
                case "工艺参数\\锁付控制":  ShowScrewParams(); break;
                case "工艺参数\\视觉定位":  ShowVisionParams(); break;
                case "工艺参数\\激光打标":  ShowLaserParams(); break;
                case "工艺参数\\干冰清洗":  ShowDryIceParams(); break;
                case "工艺参数\\组装压合":  ShowAssemblyParams(); break;
                case "信息化参数\\MES上传":  ShowMESParams(); break;
                case "信息化参数\\IFMS配置": ShowIFMSParams(); break;
                case "信息化参数\\数据追溯": ShowTraceParams(); break;
                case "信息化参数\\条码规则": ShowBarcodeParams(); break;
                case "高级\\算法参数":       ShowAlgorithmParams(); break;
                case "高级\\标定参数":       ShowCalibrationParams(); break;
                case "高级\\诊断日志":       ShowDiagnosticParams(); break;
                default: ShowFunctionParams(); break;
            }
        }

        // ============================================================
        //  各分类参数
        // ============================================================

        private void ShowFunctionParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("打压气压", "0.50", "float", "MPa"),
                ("单步显示运行时间", "False", "bool", "是否显示每步耗时"),
                ("镜头扫码", "True", "bool", "启用镜头扫码"),
                ("码规则校验", "True", "bool", "校验条码格式"),
                ("屏蔽视觉调试", "False", "bool", ""),
                ("屏蔽流道调试", "False", "bool", ""),
                ("仅流道调试", "False", "bool", ""),
                ("下游允许进料(虚拟)", "True", "bool", "虚拟I/O信号"),
                ("下游进料完成(虚拟)", "True", "bool", "虚拟I/O信号"),
                ("拍照失败仅暂停", "True", "bool", "失败后是否暂停"),
                ("MES追溯", "True", "bool", "启用MES数据跟踪"),
                ("拍整盘料", "False", "bool", ""),
                ("NG盘无需确认", "False", "bool", ""),
                ("流道常开", "True", "bool", "流道默认开启"),
                ("屏蔽扫码真空", "False", "bool", ""),
                ("自动复检", "True", "bool", "NG品自动复测"),
                ("复检次数上限", "3", "int", "最大复检次数"),
                ("空跑模式", "False", "bool", "不实际执行动作"),
                ("强制点检", "True", "bool", "班次开始强制点检"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 190, 90, 60, 200 }, items);
        }

        private void ShowAxisSpeedParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("X轴速度", "100", "float", "mm/s"),
                ("X轴加速度", "500", "float", "mm/s²"),
                ("X轴减速", "500", "float", "mm/s²"),
                ("X轴JOG速度", "20", "float", "点动速度 mm/s"),
                ("Z1轴速度", "50", "float", "mm/s"),
                ("Z1轴加速度", "300", "float", "mm/s²"),
                ("Z2轴速度", "50", "float", "mm/s"),
                ("Z5轴速度", "40", "float", "上料轴 mm/s"),
                ("Z6轴速度", "35", "float", "NG轴 mm/s"),
                ("R轴速度", "30", "float", "旋转轴 °/s"),
                ("R轴加速度", "200", "float", "°/s²"),
                ("点胶轴U速度", "25", "float", "mm/s"),
                ("回零点速度", "10", "float", "归零慢速"),
                ("快速移动速度", "200", "float", "最大速度"),
                ("软限位正(Z1)", "150.0", "float", "mm"),
                ("软限位负(Z1)", "-10.0", "float", "mm"),
                ("软限位正(X)", "300.0", "float", "mm"),
                ("软限位负(X)", "-10.0", "float", "mm"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 190, 90, 60, 200 }, items);
        }

        private void ShowIOMappingParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("X0 急停", "NC", "DI", "常闭"),
                ("X1 安全门1", "NC", "DI", "前门"),
                ("X2 安全门2", "NC", "DI", "后门"),
                ("X3 光栅", "NO", "DI", "安全光幕"),
                ("X4 真空检测1", "NO", "DI", "上料真空"),
                ("X5 真空检测2", "NO", "DI", "取料真空"),
                ("Y0 红灯", "True", "DO", "三色灯"),
                ("Y1 黄灯", "True", "DO", "三色灯"),
                ("Y2 绿灯", "True", "DO", "三色灯"),
                ("Y3 蜂鸣器", "True", "DO", "报警"),
                ("Y4 光源1", "True", "DO", "MTF光源"),
                ("Y5 光源2", "True", "DO", "对准激光"),
                ("Y6 UV灯", "True", "DO", "固化"),
                ("Y7 真空阀1", "True", "DO", ""),
                ("Y10 点胶阀", "True", "DO", ""),
                ("Y11 加热器", "True", "DO", "包边"),
                ("Y12 干冰阀", "True", "DO", "清洗"),
                ("Y13 排气阀", "True", "DO", "除尘"),
            };
            DrawTable(new[] { "地址", "值", "类型", "说明" }, new[] { 150, 90, 60, 240 }, items);
        }

        private void ShowCommParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("PLC IP", "192.168.1.100", "string", ""),
                ("PLC 端口", "502", "int", "Modbus TCP"),
                ("PLC 从站ID", "1", "int", ""),
                ("PLC 超时(ms)", "1000", "int", ""),
                ("COM3(扫码器)", "9600,N,8,1", "string", ""),
                ("COM4(微压)", "9600,N,8,1", "string", ""),
                ("COM5(电子秤)", "9600,N,8,1", "string", ""),
                ("COM6(激光)", "115200,N,8,1", "string", ""),
                ("Modbus TCP(温控)", "192.168.1.101:502", "string", ""),
                ("EtherNet/IP(视觉)", "192.168.1.200", "string", "相机1"),
                ("MES HTTP", "http://mes.local/api", "string", ""),
                ("IFMS HTTP", "http://ifms.local/ws", "string", ""),
                ("心跳间隔(s)", "5", "int", ""),
                ("重连次数", "3", "int", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 190, 180, 60, 180 }, items);
        }

        private void ShowLeakTestParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("泄漏判定阈值(Pa)", "50", "float", "压差法判定值"),
                ("保压测试时间(s)", "10", "float", ""),
                ("抽真空时间(s)", "5", "float", ""),
                ("稳定等待时间(s)", "3", "float", "压力稳定"),
                ("真空目标压力(kPa)", "-80", "float", ""),
                ("释放速度", "缓慢", "enum", "缓慢/快速"),
                ("密封检查", "True", "bool", ""),
                ("异常重试次数", "2", "int", ""),
                ("压力传感器量程(kPa)", "-100~100", "string", ""),
                ("压力采样频率(Hz)", "100", "int", ""),
                ("曲线记录", "True", "bool", "保存压降曲线"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 110, 60, 230 }, items);
        }

        private void ShowDispenseParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("点胶路径内圈", "Circle-1", "string", "路径模板"),
                ("点胶路径外圈", "Circle-2", "string", "路径模板"),
                ("点胶速度(mm/s)", "30", "float", ""),
                ("点胶量(μL)", "5", "float", "单次"),
                ("回吸量(μL)", "0.5", "float", "防止滴胶"),
                ("针头高度(mm)", "0.3", "float", "距工件"),
                ("胶水型号", "901-AB", "string", ""),
                ("胶水有效期检查", "True", "bool", ""),
                ("胶路检查", "True", "bool", "视觉检测"),
                ("胶路宽度允许偏差(%)", "±10", "string", ""),
                ("UV固化时间(s)", "5", "float", ""),
                ("UV固化功率(%)", "80", "float", ""),
                ("空胶报警阈值(μL)", "1", "float", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 110, 60, 230 }, items);
        }

        private void ShowScrewParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("目标扭矩(N·m)", "2.5", "float", ""),
                ("扭矩容差(%)", "±8", "string", ""),
                ("螺丝深度(mm)", "3.0", "float", ""),
                ("深度容差(mm)", "±0.15", "string", ""),
                ("旋入速度(rpm)", "200", "int", ""),
                ("预紧扭矩(N·m)", "0.5", "float", ""),
                ("预紧速度(rpm)", "100", "int", ""),
                ("反转松脱检测", "True", "bool", ""),
                ("滑牙检测扭矩(N·m)", "0.3", "float", "小于此值判定滑牙"),
                ("螺丝孔对位", "视觉", "enum", "视觉/机械"),
                ("锁付重试次数", "1", "int", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 110, 60, 230 }, items);
        }

        private void ShowVisionParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("定位模型", "Model-Locate-01", "string", "视觉定位模板"),
                ("检测模型", "Model-Inspect-01", "string", "缺陷检测模板"),
                ("曝光时间(μs)", "5000", "int", ""),
                ("增益", "2.0", "float", ""),
                ("光源亮度(%)", "80", "int", ""),
                ("定位超时(ms)", "3000", "int", ""),
                ("匹配分数阈值", "0.75", "float", ""),
                ("角度容差(°)", "±5", "string", ""),
                ("定位参考点X", "320", "float", "像素"),
                ("定位参考点Y", "240", "float", "像素"),
                ("ROI区域", "0,0,640,480", "string", "x,y,w,h"),
                ("图像预处理", "Median+Threshold", "string", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 150, 60, 200 }, items);
        }

        private void ShowLaserParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("激光功率(%)", "60", "int", ""),
                ("打标速度(mm/s)", "200", "int", ""),
                ("脉冲频率(kHz)", "20", "int", ""),
                ("线间距(mm)", "0.05", "float", ""),
                ("打标次数", "1", "int", ""),
                ("开光延时(μs)", "10", "int", ""),
                ("关光延时(μs)", "20", "int", ""),
                ("跳转速度(mm/s)", "800", "int", "空走"),
                ("焦距(mm)", "160", "float", ""),
                ("二维码版本", "QR_21x21", "enum", ""),
                ("纠错等级", "M", "enum", "L/M/Q/H"),
                ("文字字体", "SimSun", "string", ""),
                ("文字大小(mm)", "2.0", "float", ""),
                ("打标后验证", "True", "bool", "扫码验证"),
                ("排烟时间(s)", "2", "int", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 190, 110, 80, 220 }, items);
        }

        private void ShowDryIceParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("喷射压力(bar)", "5.0", "float", ""),
                ("流量(L/min)", "50", "int", ""),
                ("清洗时长(s)", "8", "float", "单次"),
                ("清洗次数", "1", "int", ""),
                ("喷嘴到工件距离(mm)", "30", "float", ""),
                ("旋转覆盖速度(°/s)", "45", "float", ""),
                ("压缩空气吹扫时间(s)", "3", "float", ""),
                ("排风换气时间(s)", "2", "float", ""),
                ("洁净度检测", "True", "bool", "视觉检查"),
                ("洁净度阈值", "A", "enum", "A/B/C"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 210, 110, 60, 220 }, items);
        }

        private void ShowAssemblyParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("压合力(N)", "150", "float", ""),
                ("压合速度(mm/s)", "10", "float", ""),
                ("压入深度(mm)", "12.0", "float", ""),
                ("深度公差(±mm)", "0.05", "float", ""),
                ("保压时间(s)", "1.0", "float", ""),
                ("力检测频率(Hz)", "500", "int", ""),
                ("过载保护(N)", "300", "float", "超此力急停"),
                ("对位方式", "视觉", "enum", "视觉/机械"),
                ("对位精度(μm)", "±5", "string", ""),
                ("组装后视觉检测", "True", "bool", ""),
                ("UV固化后检测", "True", "bool", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 210, 110, 60, 220 }, items);
        }

        private void ShowMESParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("MES接口地址", "http://mes.local/api", "string", ""),
                ("MES站点ID", "ST-001", "string", ""),
                ("MES超时(s)", "10", "int", ""),
                ("上传模式", "实时", "enum", "实时/批量"),
                ("上传失败重试", "3", "int", ""),
                ("上传失败暂停", "True", "bool", "失败是否暂停生产"),
                ("MES心跳间隔(s)", "30", "int", ""),
                ("认证方式", "Token", "enum", "Token/Basic"),
                ("Token密钥", "******", "string", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 180, 60, 180 }, items);
        }

        private void ShowIFMSParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("IFMS接口地址", "http://ifms.local/ws", "string", ""),
                ("IFMS产线ID", "Line-07", "string", ""),
                ("IFMS设备ID", "ZDX211-0001", "string", ""),
                ("数据上报间隔(s)", "10", "int", ""),
                ("状态上报间隔(s)", "5", "int", ""),
                ("WebSocket心跳(s)", "30", "int", ""),
                ("离线缓存", "True", "bool", "断网时本地缓存"),
                ("缓存上限(MB)", "500", "int", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 180, 60, 180 }, items);
        }

        private void ShowTraceParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("追溯粒度", "工步级", "enum", "工步级/部件级"),
                ("追溯深度(天)", "90", "int", ""),
                ("图像关联", "True", "bool", "测试图像关联追溯"),
                ("关键参数记录", "全部", "enum", "全部/结果/异常"),
                ("追溯查询接口", "SQL+API", "enum", ""),
                ("数据导出格式", "CSV+JSON", "string", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 120, 100, 200 }, items);
        }

        private void ShowBarcodeParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("条码类型", "Code128", "enum", "Code39/Code128/QR"),
                ("条码长度", "16", "int", ""),
                ("条码前缀", "SYCZ", "string", "固定前缀"),
                ("条码校验", "CRC16", "enum", "CRC16/Mod10"),
                ("条码方向", "自动", "enum", "自动/0°/90°/180°/270°"),
                ("超时(ms)", "5000", "int", ""),
                ("重复读取间隔(s)", "3", "int", ""),
                ("异常条码处理", "暂停+提示", "enum", ""),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 120, 100, 200 }, items);
        }

        private void ShowAlgorithmParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("MTF计算区域", "中心+边缘", "enum", ""),
                ("MTF边缘距离(%)", "80", "int", "距中心百分比"),
                ("MTF采样频率(lp/mm)", "100", "int", ""),
                ("偏心计算方式", "最小二乘", "enum", "最小二乘/几何中心"),
                ("偏心采样点数", "360", "int", ""),
                ("杂光分析区域", "全域", "enum", ""),
                ("杂光阈值(灰度)", "30", "int", ""),
                ("泄露压降速率(Pa/s)", "5", "float", "异常判定"),
                ("扭矩曲线拟合", "线性", "enum", "线性/多项式"),
                ("对位算法", "NCC", "enum", "NCC/ShapeMatch"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 210, 130, 80, 200 }, items);
        }

        private void ShowCalibrationParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("像素比(μm/pix)", "3.45", "float", "相机1"),
                ("像素比2(μm/pix)", "5.2", "float", "相机2"),
                ("Z1零点(mm)", "0.000", "float", ""),
                ("Z2零点(mm)", "0.000", "float", ""),
                ("X零点(mm)", "0.000", "float", ""),
                ("R零点(°)", "0.00", "float", ""),
                ("压力传感器零点(kPa)", "0.0", "float", ""),
                ("温度传感器零点(℃)", "25.0", "float", ""),
                ("电子秤零点(g)", "0.0", "float", ""),
                ("电子秤量程(g)", "200", "float", ""),
                ("激光测距零点(mm)", "0.00", "float", ""),
                ("标定日期", "2024/05/20", "string", "上次标定"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 130, 60, 210 }, items);
        }

        private void ShowDiagnosticParams()
        {
            var items = new (string Name, string Value, string Type, string Desc)[]
            {
                ("调试日志级别", "INFO", "enum", "DEBUG/INFO/WARN/ERROR"),
                ("详细日志", "False", "bool", "输出完整调试信息"),
                ("PLC报文记录", "False", "bool", "记录所有PLC报文"),
                ("循环时间记录", "True", "bool", "记录CT"),
                ("异常快照", "True", "bool", "异常时保存状态"),
                ("性能监控", "True", "bool", "CPU/内存"),
                ("告警延迟(s)", "1", "float", "告警防抖"),
            };
            DrawTable(new[] { "参数名", "值", "类型", "说明" }, new[] { 200, 120, 80, 200 }, items);
        }

        // ============================================================
        //  通用表格绘制
        // ============================================================

        private void DrawTable(string[] headers, int[] widths, (string Name, string Value, string Type, string Desc)[] items)
        {
            int x = 4, headerY = 2;
            var cfg = ConfigManager.Current?.CustomSettings;

            for (int i = 0; i < headers.Length; i++)
            {
                _paramPanel.Controls.Add(new Label
                {
                    Text = headers[i], Location = new Point(x, headerY),
                    Size = new Size(widths[i], 28), BackColor = HeaderBg,
                    Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                });
                x += widths[i] + 2;
            }

            int y = 32;
            for (int row = 0; row < items.Length; row++)
            {
                var item = items[row];
                x = 4;
                Color bg = row % 2 == 0 ? WhiteBg : RowAltBg;

                // 列0: 参数名
                _paramPanel.Controls.Add(new Label
                {
                    Text = item.Name, Location = new Point(x, y), Size = new Size(widths[0], 26),
                    Font = new Font("Microsoft YaHei", 9f),
                    BackColor = bg, ForeColor = Color.FromArgb(0, 70, 140)
                });
                x += widths[0] + 2;

                // 列1: 可编辑的 TextBox
                string cfgKey = item.Name;
                string cv;
                string cfgValue = item.Value;
                if (cfg != null && cfg.TryGetValue(cfgKey, out cv))
                    cfgValue = cv;

                var txtBox = new TextBox
                {
                    Text = cfgValue, Location = new Point(x, y), Size = new Size(widths[1], 26),
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(255, 255, 220),
                    BorderStyle = BorderStyle.FixedSingle,
                    ForeColor = Color.FromArgb(50, 50, 50)
                };
                _editableFields[cfgKey] = txtBox;
                _paramPanel.Controls.Add(txtBox);
                x += widths[1] + 2;

                // 列2: 类型
                _paramPanel.Controls.Add(new Label
                {
                    Text = item.Type, Location = new Point(x, y), Size = new Size(widths[2], 26),
                    Font = new Font("Microsoft YaHei", 9f),
                    BackColor = bg, ForeColor = Color.FromArgb(100, 100, 100)
                });
                x += widths[2] + 2;

                // 列3: 说明
                _paramPanel.Controls.Add(new Label
                {
                    Text = item.Desc, Location = new Point(x, y), Size = new Size(widths[3], 26),
                    Font = new Font("Microsoft YaHei", 9f),
                    BackColor = bg, ForeColor = Color.FromArgb(100, 100, 100)
                });

                y += 28;
            }
        }

        // ============================================================
        //  保存 / 导入 / 导出
        // ============================================================

        private void ApplyParams()
        {
            try
            {
                var result = new Dictionary<string, string>();
                foreach (var kv in _editableFields)
                {
                    result[kv.Key] = kv.Value.Text.Trim();
                }

                var boot = AppBootstrap.Instance;
                if (boot != null && boot.IsInitialized)
                {
                    var cfg = ConfigManager.Current;
                    foreach (var kv in result)
                    {
                        cfg.CustomSettings[kv.Key] = kv.Value;
                    }
                    ConfigManager.Save();
                    _hintBar.Text = $"已保存 {result.Count} 项参数";
                    _hintBar.ForeColor = Color.FromArgb(60, 130, 60);
                }
                else
                {
                    _hintBar.Text = "平台未初始化，仅写入 ConfigManager 内存";
                    _hintBar.ForeColor = Color.FromArgb(200, 120, 20);
                    var cfg = ConfigManager.Current;
                    foreach (var kv in result)
                        cfg.CustomSettings[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                _hintBar.Text = "保存失败: " + ex.Message;
                _hintBar.ForeColor = Color.Red;
            }
        }

        private void ImportParams()
        {
            using (var ofd = new OpenFileDialog { Filter = "JSON 文件|*.json", Title = "导入工艺参数" })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var json = File.ReadAllText(ofd.FileName);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (dict == null || dict.Count == 0)
                    {
                        _hintBar.Text = "文件内容为空";
                        _hintBar.ForeColor = Color.Red;
                        return;
                    }

                    int updated = 0;
                    foreach (var kv in dict)
                    {
                        if (_editableFields.TryGetValue(kv.Key, out var txt))
                        {
                            txt.Text = kv.Value;
                            updated++;
                        }
                    }
                    _hintBar.Text = $"已导入 {updated}/{dict.Count} 项参数";
                    _hintBar.ForeColor = Color.FromArgb(60, 130, 60);
                }
                catch (Exception ex)
                {
                    _hintBar.Text = "导入失败: " + ex.Message;
                    _hintBar.ForeColor = Color.Red;
                }
            }
        }

        private void ExportParams()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "JSON 文件|*.json",
                FileName = $"工艺参数_{_currentSection?.Replace("\\", "_") ?? "all"}_{DateTime.Now:yyyyMMdd_HHmm}.json",
                Title = "导出工艺参数"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var kv in _editableFields)
                    {
                        dict[kv.Key] = kv.Value.Text.Trim();
                    }
                    var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                    File.WriteAllText(sfd.FileName, json);
                    _hintBar.Text = $"已导出 {dict.Count} 项参数";
                    _hintBar.ForeColor = Color.FromArgb(60, 130, 60);
                }
                catch (Exception ex)
                {
                    _hintBar.Text = "导出失败: " + ex.Message;
                    _hintBar.ForeColor = Color.Red;
                }
            }
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData() { }
    }
}