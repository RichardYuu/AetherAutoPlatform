using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Data;
using Aether.Platform.Data.Configuration;

namespace Aether.Platform.UI.Modules
{
    public class SystemConfigView : UserControl, IModuleView
    {
        public string ModuleName => "SystemConfig";
        private Panel _paramGrid;
        private readonly string[] _tabNames = { "软件信息", "硬件配置", "文件保存", "机型配置" };
        private int _currentTab = 0;

        // 每个 Tab 的可编辑控件映射：参数名 → TextBox
        private Dictionary<string, TextBox> _editableFields = new Dictionary<string, TextBox>();

        private static readonly Color HeaderBg = Color.FromArgb(230, 235, 240);
        private static readonly Color ActiveTabBg = Color.FromArgb(0, 120, 215);
        private static readonly Color InactiveTabBg = Color.FromArgb(200, 210, 220);
        private static readonly Color WhiteBg = Color.White;
        private static readonly Color RowAltBg = Color.FromArgb(248, 249, 252);

        public SystemConfigView()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            BuildLayout();
            SwitchTab(0);
        }

        private void BuildLayout()
        {
            var topBar = new Panel { Height = 42, Dock = DockStyle.Top, BackColor = Color.FromArgb(220, 225, 235), Padding = new Padding(4, 4, 4, 4) };

            int x = 4;
            for (int i = 0; i < _tabNames.Length; i++)
            {
                int idx = i;
                var t = new Label
                {
                    Text = _tabNames[i], Location = new Point(x, 4), Size = new Size(94, 32),
                    TextAlign = ContentAlignment.MiddleCenter, BackColor = InactiveTabBg,
                    ForeColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 9f),
                    Cursor = Cursors.Hand, Tag = i
                };
                t.Click += (s, ev) => { if (s is Label l && l.Tag is int tag) SwitchTab(tag); };
                topBar.Controls.Add(t);
                x += 100;
            }

            // 工具栏按钮（带实际事件）
            var btnBar = new Panel { Dock = DockStyle.Right, Width = 310, BackColor = Color.Transparent };
            int bx = 0;
            var toolbarBtns = new[] { ("重新加载", Color.FromArgb(0, 120, 215), new Action(OnReload)),
                                       ("保存到文件", Color.FromArgb(39, 174, 96), new Action(OnSaveToFile)),
                                       ("重置密码", Color.FromArgb(200, 150, 0), new Action(OnResetPassword)),
                                       ("导出配置", Color.FromArgb(80, 80, 100), new Action(OnExportConfig)) };
            foreach (var (text, color, action) in toolbarBtns)
            {
                var btn = new Button
                {
                    Text = text, Location = new Point(bx, 4), Size = new Size(72, 32),
                    BackColor = color, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 8f)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => action();
                btnBar.Controls.Add(btn);
                bx += 78;
            }
            topBar.Controls.Add(btnBar);
            Controls.Add(topBar);

            _paramGrid = new Panel { Dock = DockStyle.Fill, BackColor = WhiteBg, AutoScroll = true, Padding = new Padding(8) };
            Controls.Add(_paramGrid);
        }

        private void SwitchTab(int index)
        {
            _currentTab = index;
            foreach (Control c in ((Panel)Controls[0]).Controls)
            {
                if (c is Label l && l.Tag is int)
                {
                    bool active = (int)l.Tag == index;
                    l.BackColor = active ? ActiveTabBg : InactiveTabBg;
                    l.ForeColor = active ? Color.White : Color.FromArgb(60, 60, 60);
                }
            }

            switch (index)
            {
                case 0: ShowSoftwareInfoTab(); break;
                case 1: ShowHardwareConfigTab(); break;
                case 2: ShowFileSaveTab(); break;
                case 3: ShowModelConfigTab(); break;
            }
        }

        // ============================================================
        //  Tab 0: 软件信息
        // ============================================================

        private void ShowSoftwareInfoTab()
        {
            _paramGrid.Controls.Clear();
            _editableFields.Clear();

            var cfg = ConfigManager.Current;
            var items = new Dictionary<string, (string value, string desc)>
            {
                { "DeviceName",       (cfg?.DeviceName       ?? ConfigGet(cfg, "DeviceName", "泄露设备"), "当前设备命名") },
                { "DeviceType",       (ConfigGet(cfg, "DeviceType", "ZDX211"), "产品型号代码") },
                { "SoftwareVersion",  (ConfigGet(cfg, "SoftwareVersion", "V2.1.0"), "当前运行版本") },
                { "DeviceSerialNo",   (ConfigGet(cfg, "DeviceSerialNo", "SYCZ.ZDX211.0001-01"), "设备唯一编号") },
                { "ConfigVersion",    (ConfigGet(cfg, "ConfigVersion", "V1.0"), "参数配置版本号") },
                { "PartNumber",       (ConfigGet(cfg, "PartNumber", "CM-2024A"), "当前生产件号") },
                { "ControllerType",   (ConfigGet(cfg, "ControllerType", "PLC 汇川"), "运动控制器品牌型号") },
                { "ControllerIp",     (ConfigGet(cfg, "ControllerIp", "192.168.1.100"), "PLC 以太网地址") },
                { "ControllerMode",   (ConfigGet(cfg, "ControllerMode", "Ethernet"), "通讯协议类型") },
                { "ScannerPort",      (ConfigGet(cfg, "ScannerPort", "COM3"), "扫码器通讯端口") },
                { "ScannerBaudRate",  (ConfigGet(cfg, "ScannerBaudRate", "9600"), "扫码器波特率") },
                { "PressurePort",     (ConfigGet(cfg, "PressurePort", "COM4"), "微压传感器串口") },
                { "PressureBaudRate", (ConfigGet(cfg, "PressureBaudRate", "9600"), "微压传感器波特率") },
                { "RunMode",          (ConfigGet(cfg, "RunMode", "连续"), "单次/连续") },
                { "StartupSelfCheck", (ConfigGet(cfg, "StartupSelfCheck", "True"), "开机自动执行自检") },
                { "DataPath",         (ConfigGet(cfg, "DataPath", "D:\\AutoPlatform\\Data\\"), "数据保存路径") },
                { "ImagePath",        (ConfigGet(cfg, "ImagePath", "D:\\AutoPlatform\\Images\\"), "图像保存路径") },
                { "LogPath",          (ConfigGet(cfg, "LogPath", "D:\\AutoPlatform\\Logs\\"), "日志保存路径") },
                { "DbServer",         (ConfigGet(cfg, "DbServer", "192.168.1.50\\SQLEXPRESS"), "SQL Server 地址") },
                { "DbName",           (ConfigGet(cfg, "DbName", "AetherPlatformDB"), "数据库名称") },
                { "MesUrl",           (ConfigGet(cfg, "MesUrl", "http://mes.aether.local/api"), "MES 接口地址") },
                { "IfmsUrl",          (ConfigGet(cfg, "IfmsUrl", "http://ifms.aether.local/api"), "IFMS 接口地址") },
                { "DebugMode",        (ConfigGet(cfg, "DebugMode", "False"), "开启后显示额外调试面板") },
            };
            DrawEditableTable(items);
        }

        // ============================================================
        //  Tab 1: 硬件配置
        // ============================================================

        private void ShowHardwareConfigTab()
        {
            _paramGrid.Controls.Clear();
            _editableFields.Clear();

            var cfg = ConfigManager.Current;
            var items = new Dictionary<string, (string value, string desc)>
            {
                { "AxisX_Enabled",      (cfg != null ? ConfigGet(cfg, "AxisX_Enabled", "使能") : "使能", "取料X轴") },
                { "AxisX_Speed",        (cfg != null ? ConfigGet(cfg, "AxisX_Speed", "100") : "100", "X轴速度(mm/s)") },
                { "AxisX_Accel",        (cfg != null ? ConfigGet(cfg, "AxisX_Accel", "500") : "500", "X轴加速度(mm/s²)") },
                { "AxisZ1_Enabled",     (cfg != null ? ConfigGet(cfg, "AxisZ1_Enabled", "使能") : "使能", "龙门Z1轴") },
                { "AxisZ1_Speed",       (cfg != null ? ConfigGet(cfg, "AxisZ1_Speed", "50") : "50", "Z1轴速度(mm/s)") },
                { "AxisZ2_Enabled",     (cfg != null ? ConfigGet(cfg, "AxisZ2_Enabled", "使能") : "使能", "龙门Z2轴") },
                { "AxisZ2_Speed",       (cfg != null ? ConfigGet(cfg, "AxisZ2_Speed", "50") : "50", "Z2轴速度(mm/s)") },
                { "AxisZ5_Enabled",     (cfg != null ? ConfigGet(cfg, "AxisZ5_Enabled", "使能") : "使能", "上料工位Z5") },
                { "AxisZ6_Enabled",     (cfg != null ? ConfigGet(cfg, "AxisZ6_Enabled", "使能") : "使能", "NG工位Z6") },
                { "Scale_Enabled",      (cfg != null ? ConfigGet(cfg, "Scale_Enabled", "使能") : "使能", "重量传感器") },
                { "Scale_BaudRate",     (cfg != null ? ConfigGet(cfg, "Scale_BaudRate", "9600") : "9600", "电子秤波特率") },
                { "Laser_Enabled",      (cfg != null ? ConfigGet(cfg, "Laser_Enabled", "使能") : "使能", "位移传感器") },
                { "Laser_BaudRate",     (cfg != null ? ConfigGet(cfg, "Laser_BaudRate", "115200") : "115200", "激光测距波特率") },
                { "Thermostat_Enabled", (cfg != null ? ConfigGet(cfg, "Thermostat_Enabled", "使能") : "使能", "温度控制器") },
                { "Thermostat_Ip",      (cfg != null ? ConfigGet(cfg, "Thermostat_Ip", "192.168.1.101") : "192.168.1.101", "温控器地址") },
                { "Pressure_Enabled",   (cfg != null ? ConfigGet(cfg, "Pressure_Enabled", "使能") : "使能", "气压传感器") },
                { "Scanner_Enabled",    (cfg != null ? ConfigGet(cfg, "Scanner_Enabled", "使能") : "使能", "条码读取") },
                { "Camera1_Enabled",    (cfg != null ? ConfigGet(cfg, "Camera1_Enabled", "使能") : "使能", "上方视觉相机") },
                { "Camera1_Ip",         (cfg != null ? ConfigGet(cfg, "Camera1_Ip", "192.168.1.200") : "192.168.1.200", "相机1 IP") },
                { "Camera1_Exposure",   (cfg != null ? ConfigGet(cfg, "Camera1_Exposure", "5000") : "5000", "相机1 曝光(us)") },
                { "Camera1_Gain",       (cfg != null ? ConfigGet(cfg, "Camera1_Gain", "2.0") : "2.0", "相机1 增益") },
                { "Camera2_Enabled",    (cfg != null ? ConfigGet(cfg, "Camera2_Enabled", "禁用") : "禁用", "侧面检测相机") },
                { "PropValve_Enabled",  (cfg != null ? ConfigGet(cfg, "PropValve_Enabled", "使能") : "使能", "气动比例阀") },
                { "PropValve_Channel",  (cfg != null ? ConfigGet(cfg, "PropValve_Channel", "0") : "0", "气动比例阀通道") },
                { "Door1_Enabled",      (cfg != null ? ConfigGet(cfg, "Door1_Enabled", "使能") : "使能", "前安全门") },
                { "Door2_Enabled",      (cfg != null ? ConfigGet(cfg, "Door2_Enabled", "使能") : "使能", "后安全门") },
                { "Estop_Enabled",      (cfg != null ? ConfigGet(cfg, "Estop_Enabled", "使能") : "使能", "急停按钮") },
                { "LightCurtain_Enabled",(cfg != null ? ConfigGet(cfg, "LightCurtain_Enabled", "使能") : "使能", "安全光幕") },
                { "TowerLight_Enabled", (cfg != null ? ConfigGet(cfg, "TowerLight_Enabled", "使能") : "使能", "状态指示灯") },
                { "Buzzer_Enabled",     (cfg != null ? ConfigGet(cfg, "Buzzer_Enabled", "使能") : "使能", "报警声音") },
            };
            DrawEditableTable(items);
        }

        // ============================================================
        //  Tab 2: 文件保存
        // ============================================================

        private void ShowFileSaveTab()
        {
            _paramGrid.Controls.Clear();
            _editableFields.Clear();

            var cfg = ConfigManager.Current;
            var items = new Dictionary<string, (string value, string desc)>
            {
                { "SaveTrayRecog",       (cfg != null ? ConfigGet(cfg, "SaveTrayRecog", "True") : "True", "保存料盘识别结果") },
                { "SaveProductId",       (cfg != null ? ConfigGet(cfg, "SaveProductId", "True") : "True", "保存产品编号记录") },
                { "SaveMTFImage",        (cfg != null ? ConfigGet(cfg, "SaveMTFImage", "True") : "True", "保存MTF测试原图") },
                { "SaveMTFResult",       (cfg != null ? ConfigGet(cfg, "SaveMTFResult", "True") : "True", "保存MTF计算数值") },
                { "SaveEccentricImage",  (cfg != null ? ConfigGet(cfg, "SaveEccentricImage", "False") : "False", "保存偏心测试原图") },
                { "SaveEccentricResult", (cfg != null ? ConfigGet(cfg, "SaveEccentricResult", "True") : "True", "保存偏心计算结果") },
                { "SaveStrayLightImage", (cfg != null ? ConfigGet(cfg, "SaveStrayLightImage", "True") : "True", "保存杂光检测原图") },
                { "SaveLeakCurve",       (cfg != null ? ConfigGet(cfg, "SaveLeakCurve", "True") : "True", "保存泄露测试曲线") },
                { "SaveDispenseImage",   (cfg != null ? ConfigGet(cfg, "SaveDispenseImage", "False") : "False", "保存点胶视觉检测图") },
                { "ImageRetentionDays",  (cfg != null ? ConfigGet(cfg, "ImageRetentionDays", "30") : "30", "图片保存期限(天)") },
                { "DataRetentionDays",   (cfg != null ? ConfigGet(cfg, "DataRetentionDays", "90") : "90", "数据保存期限(天)") },
                { "ImagePassword",       (cfg != null ? ConfigGet(cfg, "ImagePassword", "****") : "****", "图片访问密码") },
                { "ChecklistRefresh",    (cfg != null ? ConfigGet(cfg, "ChecklistRefresh", "60") : "60", "点检刷新间隔(s)") },
                { "MesUploadMode",       (cfg != null ? ConfigGet(cfg, "MesUploadMode", "实时") : "实时", "MES上传模式") },
                { "LocalBackup",         (cfg != null ? ConfigGet(cfg, "LocalBackup", "True") : "True", "本地双备份") },
                { "BackupPath",          (cfg != null ? ConfigGet(cfg, "BackupPath", "D:\\Backup\\") : "D:\\Backup\\", "备份存放目录") },
                { "CompressArchive",     (cfg != null ? ConfigGet(cfg, "CompressArchive", "True") : "True", "压缩归档") },
                { "ArchivePath",         (cfg != null ? ConfigGet(cfg, "ArchivePath", "D:\\Archive\\") : "D:\\Archive\\", "归档存放目录") },
                { "LogRetentionDays",    (cfg != null ? ConfigGet(cfg, "LogRetentionDays", "30") : "30", "日志保留天数") },
                { "LogMaxSize",          (cfg != null ? ConfigGet(cfg, "LogMaxSize", "500") : "500", "日志最大大小(MB)") },
            };
            DrawEditableTable(items);
        }

        // ============================================================
        //  Tab 3: 机型配置
        // ============================================================

        private void ShowModelConfigTab()
        {
            _paramGrid.Controls.Clear();
            _editableFields.Clear();

            var cfg = ConfigManager.Current;
            var items = new Dictionary<string, (string value, string desc)>
            {
                { "ModelCode",           (cfg != null ? ConfigGet(cfg, "ModelCode", "CM-2024A") : "CM-2024A", "当前机型代码") },
                { "ModelName",           (cfg != null ? ConfigGet(cfg, "ModelName", "Compact Module 2024A") : "Compact Module 2024A", "机型全称") },
                { "LensSpec",            (cfg != null ? ConfigGet(cfg, "LensSpec", "Φ16mm") : "Φ16mm", "被测镜头规格") },
                { "MTFThreshold",        (cfg != null ? ConfigGet(cfg, "MTFThreshold", "0.65") : "0.65", "MTF合格最小值") },
                { "MTFEdgeThreshold",    (cfg != null ? ConfigGet(cfg, "MTFEdgeThreshold", "0.45") : "0.45", "边缘MTF合格最小值") },
                { "EccentricTolerance",  (cfg != null ? ConfigGet(cfg, "EccentricTolerance", "5.0") : "5.0", "最大允许偏心量(μm)") },
                { "TiltTolerance",       (cfg != null ? ConfigGet(cfg, "TiltTolerance", "3.0") : "3.0", "最大允许倾斜角(′)") },
                { "StrayLightGrade",     (cfg != null ? ConfigGet(cfg, "StrayLightGrade", "A") : "A", "杂光评级 A/B/C") },
                { "LeakThreshold",       (cfg != null ? ConfigGet(cfg, "LeakThreshold", "50") : "50", "泄露阈值(Pa)") },
                { "LeakTestTime",        (cfg != null ? ConfigGet(cfg, "LeakTestTime", "10") : "10", "保压测试时长(s)") },
                { "DispenseInnerPath",   (cfg != null ? ConfigGet(cfg, "DispenseInnerPath", "Circle-1") : "Circle-1", "内圈点胶路径") },
                { "DispenseOuterPath",   (cfg != null ? ConfigGet(cfg, "DispenseOuterPath", "Circle-2") : "Circle-2", "外圈点胶路径") },
                { "DispenseSpeed",       (cfg != null ? ConfigGet(cfg, "DispenseSpeed", "30") : "30", "点胶速度(mm/s)") },
                { "DispenseVolume",      (cfg != null ? ConfigGet(cfg, "DispenseVolume", "5") : "5", "单次点胶量(uL)") },
                { "UvCureTime",          (cfg != null ? ConfigGet(cfg, "UvCureTime", "5") : "5", "UV固化时间(s)") },
                { "WrapTemp",            (cfg != null ? ConfigGet(cfg, "WrapTemp", "180") : "180", "包边温度(℃)") },
                { "WrapPressure",        (cfg != null ? ConfigGet(cfg, "WrapPressure", "200") : "200", "包边压力(N)") },
                { "WrapHoldTime",        (cfg != null ? ConfigGet(cfg, "WrapHoldTime", "8") : "8", "包边保压时间(s)") },
                { "ScrewTorque",         (cfg != null ? ConfigGet(cfg, "ScrewTorque", "2.5") : "2.5", "锁付扭矩(N·m)") },
                { "LaserPower",          (cfg != null ? ConfigGet(cfg, "LaserPower", "60") : "60", "激光功率(%)") },
                { "LaserSpeed",          (cfg != null ? ConfigGet(cfg, "LaserSpeed", "200") : "200", "打标速度(mm/s)") },
                { "DryIcePressure",      (cfg != null ? ConfigGet(cfg, "DryIcePressure", "5.0") : "5.0", "干冰压力(bar)") },
                { "DryIceDuration",      (cfg != null ? ConfigGet(cfg, "DryIceDuration", "8") : "8", "干冰清洗时长(s)") },
                { "AssemblyForce",       (cfg != null ? ConfigGet(cfg, "AssemblyForce", "150") : "150", "组装压合力(N)") },
                { "AssemblyDepthTol",    (cfg != null ? ConfigGet(cfg, "AssemblyDepthTol", "0.05") : "0.05", "压装深度公差(mm)") },
            };
            DrawEditableTable(items);
        }

        // ============================================================
        //  辅助方法
        // ============================================================

        private string ConfigGet(AppConfig cfg, string key, string defaultVal)
        {
            if (cfg?.CustomSettings != null && cfg.CustomSettings.TryGetValue(key, out string val))
                return val;
            return defaultVal;
        }

        // ============================================================
        //  可编辑参数表格
        // ============================================================

        private void DrawEditableTable(Dictionary<string, (string value, string desc)> items)
        {
            var headers = new[] { "参数名称", "值", "参数说明" };
            var widths = new[] { 200, 180, 240 };
            int x = 6, headerY = 6;

            // 表头
            for (int i = 0; i < headers.Length; i++)
            {
                _paramGrid.Controls.Add(new Label
                {
                    Text = headers[i], Location = new Point(x, headerY),
                    Size = new Size(widths[i], 30), BackColor = HeaderBg,
                    Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft
                });
                x += widths[i] + 2;
            }

            // 数据行（值列可编辑）
            int y = 38;
            foreach (var kv in items)
            {
                string key = kv.Key;
                string value = kv.Value.value;
                string desc = kv.Value.desc;

                x = 6;
                Color rowBg = _paramGrid.Controls.Count % 2 == 0 ? WhiteBg : RowAltBg;

                // 参数名
                _paramGrid.Controls.Add(new Label
                {
                    Text = key, Location = new Point(x, y), Size = new Size(widths[0], 28),
                    Font = new Font("Consolas", 9f), BackColor = rowBg,
                    ForeColor = Color.FromArgb(0, 70, 140)
                });
                x += widths[0] + 2;

                // 值（可编辑 TextBox）
                var txt = new TextBox
                {
                    Text = value, Location = new Point(x, y), Size = new Size(widths[1], 28),
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(255, 255, 240), BorderStyle = BorderStyle.FixedSingle,
                    ForeColor = Color.FromArgb(50, 50, 50)
                };
                _editableFields[key] = txt;
                _paramGrid.Controls.Add(txt);
                x += widths[1] + 2;

                // 说明
                _paramGrid.Controls.Add(new Label
                {
                    Text = desc, Location = new Point(x, y), Size = new Size(widths[2], 28),
                    Font = new Font("Microsoft YaHei", 9f), BackColor = rowBg,
                    ForeColor = Color.FromArgb(100, 100, 110)
                });
                y += 30;
            }

            // 底部操作按钮
            var btnPanel = new Panel
            {
                Location = new Point(6, y + 10), Size = new Size(500, 40),
                BackColor = Color.Transparent
            };

            var saveBtn = new Button
            {
                Text = "保存修改",
                Location = new Point(0, 4), Size = new Size(100, 32),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold)
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) => OnSaveCurrentTab();
            btnPanel.Controls.Add(saveBtn);

            var resetBtn = new Button
            {
                Text = "恢复默认",
                Location = new Point(110, 4), Size = new Size(100, 32),
                BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold)
            };
            resetBtn.FlatAppearance.BorderSize = 0;
            resetBtn.Click += (s, e) => OnResetToDefault();
            btnPanel.Controls.Add(resetBtn);

            _paramGrid.Controls.Add(btnPanel);
        }

        // ============================================================
        //  操作逻辑
        // ============================================================

        private void OnSaveCurrentTab()
        {
            var cfg = ConfigManager.Current;
            if (cfg == null)
            {
                MessageBox.Show("ConfigManager 未初始化，无法保存配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cfg.CustomSettings == null)
                cfg.CustomSettings = new Dictionary<string, string>();

            int saved = 0;
            foreach (var kv in _editableFields)
            {
                string newVal = kv.Value.Text.Trim();
                cfg.CustomSettings[kv.Key] = newVal;
                saved++;
            }

            ConfigManager.Save();
            MessageBox.Show($"已保存 {saved} 项配置参数。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnReload()
        {
            ConfigManager.Load();
            SwitchTab(_currentTab);
            MessageBox.Show("已从文件重新加载配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnSaveToFile()
        {
            using (var sfd = new SaveFileDialog
            {
                Filter = "JSON 配置文件|*.json",
                FileName = "config_backup.json",
                DefaultExt = ".json"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ConfigManager.SaveToPath(sfd.FileName);
                    MessageBox.Show($"配置已保存到:\n{sfd.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void OnResetPassword()
        {
            var result = MessageBox.Show("确认重置组长密码为默认密码吗？", "重置密码", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                var cfg = ConfigManager.Current;
                if (cfg != null)
                {
                    if (cfg.CustomSettings == null)
                        cfg.CustomSettings = new Dictionary<string, string>();
                    cfg.CustomSettings["SupervisorPassword"] = "123456";
                    ConfigManager.Save();
                }
                MessageBox.Show("组长密码已重置为 123456", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnExportConfig()
        {
            OnSaveToFile();
        }

        private void OnResetToDefault()
        {
            var result = MessageBox.Show("确认恢复当前页所有参数为默认值吗？\n此操作不可撤销！", "确认恢复默认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                SwitchTab(_currentTab);
                MessageBox.Show("已恢复默认值（尚未保存，请点击保存修改确认。）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void OnActivated()
        {
            SwitchTab(_currentTab);
        }

        public void OnDeactivated() { }

        public void RefreshData()
        {
            ConfigManager.Load();
            SwitchTab(_currentTab);
        }
    }
}