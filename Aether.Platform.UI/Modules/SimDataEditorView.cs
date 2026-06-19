using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.UI.Modules
{
    public class SimDataEditorView : UserControl, IModuleView
    {
        public string ModuleName => "SimDataEditor";

        private Panel _leftPanel;
        private Panel _rightPanel;
        private ListBox _categoryList;
        private Panel _editorArea;
        private Button _btnApply;
        private string _currentCategory;
        private readonly SimDataStore _store = SimDataStore.Instance;

        // 缓存当前类别的编辑控件
        private Dictionary<string, object> _currentEditors = new Dictionary<string, object>();

        private static readonly Color PanelBg = Color.FromArgb(245, 246, 248);
        private static readonly Color AccentBlue = Color.FromArgb(0, 120, 215);
        private static readonly Color EditBg = Color.White;

        public SimDataEditorView()
        {
            BackColor = PanelBg;
            BuildUI();
        }

        private void BuildUI()
        {
            // 左侧类别列表
            _leftPanel = new Panel
            {
                Width = 160, Dock = DockStyle.Left,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "数据类别",
                Dock = DockStyle.Top, Height = 32,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(235, 240, 245)
            };

            _categoryList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 10f),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            _categoryList.Items.AddRange(new[] { "轴位置", "数字 IO", "PLC 寄存器", "PLC 继电器", "传感器", "模拟量通道", "扫码器", "视觉参数" });
            _categoryList.SelectedIndex = 0;
            _categoryList.SelectedIndexChanged += OnCategoryChanged;

            _leftPanel.Controls.Add(_categoryList);
            _leftPanel.Controls.Add(lblTitle);

            // 右侧编辑区
            _rightPanel = new Panel
            {
                Dock = DockStyle.Fill, BackColor = PanelBg,
                Padding = new Padding(12, 8, 12, 8)
            };

            _editorArea = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            _btnApply = new Button
            {
                Text = "应用修改",
                Dock = DockStyle.Bottom, Height = 36,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                BackColor = AccentBlue, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            _btnApply.FlatAppearance.BorderSize = 0;
            _btnApply.Click += (s, e) => OnApply();

            _rightPanel.Controls.Add(_editorArea);
            _rightPanel.Controls.Add(_btnApply);

            Controls.Add(_rightPanel);
            Controls.Add(_leftPanel);
        }

        private void OnCategoryChanged(object sender, EventArgs e)
        {
            _currentCategory = _categoryList.SelectedItem?.ToString();
            RenderCategory();
        }

        private void RenderCategory()
        {
            _editorArea.Controls.Clear();
            _currentEditors.Clear();
            if (string.IsNullOrEmpty(_currentCategory)) return;

            int y = 4;
            switch (_currentCategory)
            {
                case "轴位置":    RenderAxes(ref y);       break;
                case "数字 IO":   RenderDio(ref y);        break;
                case "PLC 寄存器":RenderPlcD(ref y);       break;
                case "PLC 继电器":RenderPlcM(ref y);       break;
                case "传感器":    RenderSensors(ref y);    break;
                case "模拟量通道":RenderAnalog(ref y);     break;
                case "扫码器":    RenderScanner(ref y);    break;
                case "视觉参数":  RenderVision(ref y);     break;
            }
        }

        private void RenderAxes(ref int y)
        {
            AddSectionHeader("轴位置 & 速度", ref y);
            foreach (var kv in _store.Axes)
            {
                var axis = kv.Value;
                y = AddAxisRow(kv.Key, axis, y);
            }
        }

        private int AddAxisRow(string id, SimAxisData axis, int y)
        {
            var panel = new Panel { Location = new Point(4, y), Size = new Size(660, 38), BackColor = Color.Transparent };

            var lbl = new Label
            {
                Text = axis.Name, Location = new Point(4, 8), Size = new Size(100, 22),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
            };

            var txtPos = new TextBox
            {
                Location = new Point(110, 6), Size = new Size(70, 26),
                Font = new Font("Consolas", 10f), Text = axis.Position.ToString("F1"),
                Tag = new { Axis = id, Field = "Position" }
            };

            var lblPos = new Label { Text = "mm", Location = new Point(184, 9), AutoSize = true, Font = new Font("Microsoft YaHei", 8f), ForeColor = Color.Gray };

            var txtSpeed = new TextBox
            {
                Location = new Point(220, 6), Size = new Size(60, 26),
                Font = new Font("Consolas", 10f), Text = axis.Speed.ToString("F0"),
                Tag = new { Axis = id, Field = "Speed" }
            };

            var lblSpd = new Label { Text = "mm/s", Location = new Point(284, 9), AutoSize = true, Font = new Font("Microsoft YaHei", 8f), ForeColor = Color.Gray };

            var cbHome = new CheckBox
            {
                Text = "原点", Location = new Point(330, 8), Size = new Size(55, 20),
                Checked = axis.IsHomed, Font = new Font("Microsoft YaHei", 9f),
                Tag = new { Axis = id, Field = "IsHomed" }
            };

            var cbEn = new CheckBox
            {
                Text = "使能", Location = new Point(390, 8), Size = new Size(55, 20),
                Checked = axis.IsEnabled, Font = new Font("Microsoft YaHei", 9f),
                Tag = new { Axis = id, Field = "IsEnabled" }
            };

            panel.Controls.AddRange(new Control[] { lbl, txtPos, lblPos, txtSpeed, lblSpd, cbHome, cbEn });
            _currentEditors[id + "_Position"] = txtPos;
            _currentEditors[id + "_Speed"] = txtSpeed;
            _currentEditors[id + "_IsHomed"] = cbHome;
            _currentEditors[id + "_IsEnabled"] = cbEn;

            _editorArea.Controls.Add(panel);
            return y + 40;
        }

        private void RenderDio(ref int y)
        {
            AddSectionHeader("数字 IO 输入", ref y);
            for (int i = 0; i < 8; i++)
                y = AddDioRow(i, true, y);

            y += 8;
            AddSectionHeader("数字 IO 输出", ref y);
            for (int i = 0; i < 8; i++)
                y = AddDioRow(i, false, y);
        }

        private int AddDioRow(int index, bool isInput, int y)
        {
            var data = isInput ? _store.Inputs[index] : _store.Outputs[index];
            var cb = new CheckBox
            {
                Text = $"{data.Name}",
                Location = new Point(10, y), Size = new Size(130, 22),
                Checked = data.Value, Font = new Font("Microsoft YaHei", 9f),
                Tag = new { IoType = isInput ? "Input" : "Output", Index = index }
            };
            var statusDot = new Label
            {
                Text = data.Value ? "●" : "○",
                Location = new Point(140, y + 1), AutoSize = true,
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = data.Value ? Color.Green : Color.Gray
            };

            _currentEditors[$"DIO_{(isInput ? "I" : "O")}{index}"] = cb;

            _editorArea.Controls.Add(cb);
            _editorArea.Controls.Add(statusDot);
            cb.CheckedChanged += (s, e) => statusDot.ForeColor = cb.Checked ? Color.Green : Color.Gray;
            return y + 24;
        }

        private void RenderPlcD(ref int y)
        {
            AddSectionHeader("PLC D 寄存器", ref y);
            foreach (var kv in _store.PlcDRegisters.OrderBy(k => k.Key))
            {
                var lbl = new Label
                {
                    Text = kv.Key, Location = new Point(10, y + 4), Size = new Size(70, 22),
                    Font = new Font("Consolas", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
                };
                var txt = new TextBox
                {
                    Location = new Point(85, y), Size = new Size(80, 26),
                    Font = new Font("Consolas", 10f), Text = kv.Value.ToString(),
                    Tag = new { Address = kv.Key }
                };
                _currentEditors["PLC_D_" + kv.Key] = txt;

                _editorArea.Controls.Add(lbl);
                _editorArea.Controls.Add(txt);
                y += 30;
            }
        }

        private void RenderPlcM(ref int y)
        {
            AddSectionHeader("PLC M 继电器", ref y);
            foreach (var kv in _store.PlcMBits.OrderBy(k => k.Key))
            {
                var cb = new CheckBox
                {
                    Text = kv.Key, Location = new Point(10, y), Size = new Size(120, 24),
                    Checked = kv.Value, Font = new Font("Consolas", 10f), Tag = new { Address = kv.Key }
                };
                _currentEditors["PLC_M_" + kv.Key] = cb;
                _editorArea.Controls.Add(cb);
                y += 26;
            }
        }

        private void RenderSensors(ref int y)
        {
            AddSectionHeader("传感器模拟值", ref y);
            foreach (var kv in _store.SensorValues)
            {
                var lbl = new Label
                {
                    Text = kv.Key, Location = new Point(10, y + 4), Size = new Size(100, 22),
                    Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
                };
                var txt = new TextBox
                {
                    Location = new Point(115, y), Size = new Size(80, 26),
                    Font = new Font("Consolas", 10f), Text = kv.Value.ToString("F1"),
                    Tag = new { Sensor = kv.Key }
                };
                _currentEditors["SENSOR_" + kv.Key] = txt;
                _editorArea.Controls.Add(lbl);
                _editorArea.Controls.Add(txt);
                y += 30;
            }
        }

        private void RenderAnalog(ref int y)
        {
            AddSectionHeader("模拟量通道", ref y);
            foreach (var kv in _store.AnalogChannels.OrderBy(k => k.Key))
            {
                var lbl = new Label
                {
                    Text = $"CH{kv.Key}", Location = new Point(10, y + 4), Size = new Size(70, 22),
                    Font = new Font("Consolas", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
                };
                var txt = new TextBox
                {
                    Location = new Point(85, y), Size = new Size(80, 26),
                    Font = new Font("Consolas", 10f), Text = kv.Value.ToString("F1"),
                    Tag = new { Channel = kv.Key }
                };
                _currentEditors["ANALOG_" + kv.Key] = txt;
                _editorArea.Controls.Add(lbl);
                _editorArea.Controls.Add(txt);
                y += 30;
            }
        }

        private void RenderScanner(ref int y)
        {
            AddSectionHeader("扫码器", ref y);

            var lblRate = new Label { Text = "成功率", Location = new Point(10, y + 5), Size = new Size(80, 22), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
            var txtRate = new TextBox
            {
                Location = new Point(95, y), Size = new Size(60, 26),
                Font = new Font("Consolas", 10f), Text = (_store.ScannerSuccessRate * 100).ToString("F0"),
                Tag = "ScannerRate"
            };
            var lblPct = new Label { Text = "%", Location = new Point(160, y + 5), AutoSize = true, Font = new Font("Microsoft YaHei", 9f) };
            _currentEditors["ScannerRate"] = txtRate;

            y += 32;

            var lblBarcode = new Label { Text = "预设条码", Location = new Point(10, y + 5), Size = new Size(80, 22), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
            var txtBarcode = new TextBox
            {
                Location = new Point(95, y), Size = new Size(160, 26),
                Font = new Font("Consolas", 10f), Text = _store.NextBarcode,
                Tag = "NextBarcode"
            };
            var lblHint = new Label { Text = "留空=随机生成", Location = new Point(262, y + 5), AutoSize = true, Font = new Font("Microsoft YaHei", 8f), ForeColor = Color.Gray };
            _currentEditors["NextBarcode"] = txtBarcode;

            _editorArea.Controls.Add(lblRate); _editorArea.Controls.Add(txtRate); _editorArea.Controls.Add(lblPct);
            _editorArea.Controls.Add(lblBarcode); _editorArea.Controls.Add(txtBarcode); _editorArea.Controls.Add(lblHint);
        }

        private void RenderVision(ref int y)
        {
            AddSectionHeader("视觉定位结果", ref y);
            var items = new[]
            {
                new { Label = "匹配分数", Key = "VisionScore", Value = (_store.VisionScore * 100).ToString("F1"), Unit = "%" },
                new { Label = "X 坐标",    Key = "VisionX",     Value = _store.VisionX.ToString("F1"), Unit = "px" },
                new { Label = "Y 坐标",    Key = "VisionY",     Value = _store.VisionY.ToString("F1"), Unit = "px" },
                new { Label = "角度",      Key = "VisionAngle", Value = _store.VisionAngle.ToString("F1"), Unit = "°" },
            };

            foreach (var item in items)
            {
                var lbl = new Label { Text = item.Label, Location = new Point(10, y + 4), Size = new Size(90, 22), Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold) };
                var txt = new TextBox { Location = new Point(105, y), Size = new Size(80, 26), Font = new Font("Consolas", 10f), Text = item.Value, Tag = item.Key };
                var unit = new Label { Text = item.Unit, Location = new Point(190, y + 4), AutoSize = true, Font = new Font("Microsoft YaHei", 9f) };
                _currentEditors[item.Key] = txt;
                _editorArea.Controls.Add(lbl); _editorArea.Controls.Add(txt); _editorArea.Controls.Add(unit);
                y += 30;
            }
        }

        private void AddSectionHeader(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = "▎" + text,
                Location = new Point(4, y), Size = new Size(600, 26),
                ForeColor = AccentBlue,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _editorArea.Controls.Add(lbl);
            y += 30;
        }

        private void OnApply()
        {
            try
            {
                switch (_currentCategory)
                {
                    case "轴位置":
                        ApplyAxes();
                        break;
                    case "数字 IO":
                        ApplyDio();
                        break;
                    case "PLC 寄存器":
                        ApplyPlcD();
                        break;
                    case "PLC 继电器":
                        ApplyPlcM();
                        break;
                    case "传感器":
                    case "模拟量通道":
                        ApplyDictionary();
                        break;
                    case "扫码器":
                        ApplyScanner();
                        break;
                    case "视觉参数":
                        ApplyVision();
                        break;
                }
                MessageBox.Show("仿真数据已更新", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据格式错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyAxes()
        {
            foreach (var kv in _currentEditors)
            {
                var parts = kv.Key.Split('_');
                if (parts.Length < 2) continue;
                string axisId = parts[1];
                string field = parts[2];

                if (_store.Axes.TryGetValue(axisId, out var axis))
                {
                    if (kv.Value is TextBox txt)
                    {
                        if (field == "Position") axis.Position = double.Parse(txt.Text);
                        else if (field == "Speed") axis.Speed = double.Parse(txt.Text);
                    }
                    else if (kv.Value is CheckBox cb)
                    {
                        if (field == "IsHomed") axis.IsHomed = cb.Checked;
                        else if (field == "IsEnabled") axis.IsEnabled = cb.Checked;
                    }
                }
            }
        }

        private void ApplyDio()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is CheckBox cb)) continue;
                var parts = kv.Key.Split('_');
                if (parts.Length < 2) continue;
                string type = parts[1]; // "I" or "O"
                int index = int.Parse(parts[2]);

                if (type == "I" && _store.Inputs.ContainsKey(index))
                    _store.Inputs[index].Value = cb.Checked;
                else if (type == "O" && _store.Outputs.ContainsKey(index))
                    _store.Outputs[index].Value = cb.Checked;
            }
        }

        private void ApplyPlcD()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is TextBox txt)) continue;
                string key = kv.Key.Replace("PLC_D_", "");
                _store.PlcDRegisters[key] = int.Parse(txt.Text);
            }
        }

        private void ApplyPlcM()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is CheckBox cb)) continue;
                string key = kv.Key.Replace("PLC_M_", "");
                _store.PlcMBits[key] = cb.Checked;
            }
        }

        private void ApplyDictionary()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is TextBox txt)) continue;
                string key = kv.Key.Replace("SENSOR_", "").Replace("ANALOG_", "");
                double val = double.Parse(txt.Text);

                if (kv.Key.StartsWith("SENSOR_") && _store.SensorValues.ContainsKey(key))
                    _store.SensorValues[key] = val;
                else if (kv.Key.StartsWith("ANALOG_") && _store.AnalogChannels.ContainsKey(int.Parse(key)))
                    _store.AnalogChannels[int.Parse(key)] = val;
            }
        }

        private void ApplyScanner()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is TextBox txt)) continue;
                if (kv.Key == "ScannerRate")
                    _store.ScannerSuccessRate = double.Parse(txt.Text) / 100.0;
                else if (kv.Key == "NextBarcode")
                    _store.NextBarcode = txt.Text;
            }
        }

        private void ApplyVision()
        {
            foreach (var kv in _currentEditors)
            {
                if (!(kv.Value is TextBox txt)) continue;
                double val = double.Parse(txt.Text);
                switch (kv.Key)
                {
                    case "VisionScore": _store.VisionScore = val / 100.0; break;
                    case "VisionX": _store.VisionX = val; break;
                    case "VisionY": _store.VisionY = val; break;
                    case "VisionAngle": _store.VisionAngle = val; break;
                }
            }
        }

        public void OnActivated() { }
        public void OnDeactivated() { }
        public void RefreshData()
        {
            if (_currentCategory != null)
                RenderCategory();
        }
    }
}