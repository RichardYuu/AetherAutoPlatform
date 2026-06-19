using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;

namespace Aether.Platform.UI.Modules
{
    public class VisionDebugView : UserControl, IModuleView
    {
        public string ModuleName => "VisionDebug";

        // 相机状态
        private class CameraInfo
        {
            public string Id;
            public string Name;
            public bool IsConnected;
            public string LastError;
            public int FrameCount;
            public double LastCaptureMs;
        }

        private readonly List<CameraInfo> _cameras = new List<CameraInfo>();
        private Panel _cameraListPanel;
        private readonly Label _imagePlaceholder;
        private Label _statusLabel;
        private readonly PictureBox _pictureBox;

        // 采集参数
        private NumericUpDown _exposureNum;
        private NumericUpDown _gainNum;
        private TrackBar _exposureTrack;
        private TrackBar _gainTrack;

        // 连续采集
        private bool _isContinuous;
        private CancellationTokenSource _continuousCts;
        private Button _continuousBtn;

        // ROI
        private Point _roiStart;
        private Point _roiEnd;
        private bool _isDraggingRoi;
        private Rectangle _roiRect;
        private Label _roiLabel;

        // 图像数据
        private Bitmap _currentImage;
        private readonly object _imageLock = new object();

        // 统计
        private int _totalCaptures;
        private double _totalCaptureMs;
        private Label _statsLabel;
        private Label _fpsLabel;

        // 模拟
        private readonly Random _rng = new Random();

        public VisionDebugView()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            Padding = new Padding(4);
            Dock = DockStyle.Fill;

            InitCameraList();

            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Cross
            };
            _pictureBox.Paint += OnImagePaint;
            _pictureBox.MouseDown += OnImageMouseDown;
            _pictureBox.MouseMove += OnImageMouseMove;
            _pictureBox.MouseUp += OnImageMouseUp;

            _imagePlaceholder = new Label
            {
                Text = "图像显示区\n\n— 连接相机后可实时预览 —",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 100, 110),
                Font = new Font("Microsoft YaHei", 14f),
                BackColor = Color.Transparent
            };

            _statusLabel = new Label();
            _continuousBtn = new Button();
            _roiLabel = new Label();
            _statsLabel = new Label();
            _fpsLabel = new Label();

            BuildLayout();
            PopulateLayout();
            RebuildCameraList();
        }

        private void InitCameraList()
        {
            _cameras.Add(new CameraInfo { Id = "CAM-01", Name = "相机1 — 上视定位" });
            _cameras.Add(new CameraInfo { Id = "CAM-02", Name = "相机2 — 下视检测" });
            _cameras.Add(new CameraInfo { Id = "CAM-03", Name = "相机3 — 侧视补拍" });
        }

        private void BuildLayout()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            Controls.Add(table);

            _cameraListPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(2) };
            table.Controls.Add(_cameraListPanel, 0, 0);

            var centerPnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(4), Margin = new Padding(2) };
            table.Controls.Add(centerPnl, 1, 0);

            var rightPnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), Margin = new Padding(2), AutoScroll = true };
            table.Controls.Add(rightPnl, 2, 0);

            // 左：相机列表
            _cameraListPanel.Controls.Add(new Label
            {
                Text = "相机列表",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 140),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // 中：图像区域
            if (_pictureBox != null) centerPnl.Controls.Add(_pictureBox);
            if (_imagePlaceholder != null)
            {
                centerPnl.Controls.Add(_imagePlaceholder);
                _imagePlaceholder.BringToFront();
            }

            var infoBar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(40, 40, 50) };
            _statusLabel = new Label
            {
                Text = "未连接",
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Consolas", 9f),
                Dock = DockStyle.Left,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _roiLabel = new Label
            {
                Text = "ROI: -",
                ForeColor = Color.FromArgb(140, 200, 255),
                Font = new Font("Consolas", 9f),
                Dock = DockStyle.Left,
                Width = 180,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statsLabel = new Label
            {
                Text = "采集: 0 次",
                ForeColor = Color.FromArgb(160, 220, 160),
                Font = new Font("Consolas", 9f),
                Dock = DockStyle.Left,
                Width = 150,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _fpsLabel = new Label
            {
                Text = "FPS: -",
                ForeColor = Color.FromArgb(255, 200, 100),
                Font = new Font("Consolas", 9f),
                Dock = DockStyle.Left,
                Width = 100,
                TextAlign = ContentAlignment.MiddleLeft
            };
            infoBar.Controls.Add(_fpsLabel);
            infoBar.Controls.Add(_statsLabel);
            infoBar.Controls.Add(_roiLabel);
            infoBar.Controls.Add(_statusLabel);
            centerPnl.Controls.Add(infoBar);

            // 右：控制面板
            BuildRightPanel(rightPnl);
        }

        private void BuildRightPanel(Panel pnl)
        {
            int y = 4;

            pnl.Controls.Add(MakeSectionLabel("相机控制", ref y));

            var openBtn = new Button
            {
                Text = "打开相机",
                Location = new Point(6, y),
                Size = new Size(pnl.Width - 18, 34),
                BackColor = Color.FromArgb(0, 140, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold)
            };
            openBtn.FlatAppearance.BorderSize = 0;
            openBtn.Click += (s, e) => ToggleCameraConnection();
            pnl.Controls.Add(openBtn);
            y += 42;

            // 曝光
            pnl.Controls.Add(MakeSectionLabel("曝光 (us)", ref y));
            _exposureNum = new NumericUpDown { Location = new Point(6, y), Size = new Size(80, 22), Minimum = 10, Maximum = 100000, Value = 1000, Increment = 100, TextAlign = HorizontalAlignment.Right };
            _exposureNum.ValueChanged += (s, e) => { _exposureTrack.Value = (int)_exposureNum.Value; };
            pnl.Controls.Add(_exposureNum);
            _exposureTrack = new TrackBar { Location = new Point(90, y - 2), Size = new Size(pnl.Width - 100, 26), Minimum = 10, Maximum = 100000, Value = 1000, TickFrequency = 5000 };
            _exposureTrack.ValueChanged += (s, e) => { _exposureNum.Value = _exposureTrack.Value; };
            pnl.Controls.Add(_exposureTrack);
            y += 30;

            // 增益
            pnl.Controls.Add(MakeSectionLabel("增益 (dB)", ref y));
            _gainNum = new NumericUpDown { Location = new Point(6, y), Size = new Size(80, 22), Minimum = 0, Maximum = 480, Value = 100, Increment = 10, TextAlign = HorizontalAlignment.Right, DecimalPlaces = 1 };
            _gainNum.ValueChanged += (s, e) => { _gainTrack.Value = (int)(_gainNum.Value * 10); };
            pnl.Controls.Add(_gainNum);
            _gainTrack = new TrackBar { Location = new Point(90, y - 2), Size = new Size(pnl.Width - 100, 26), Minimum = 0, Maximum = 4800, Value = 1000, TickFrequency = 500 };
            _gainTrack.ValueChanged += (s, e) => { _gainNum.Value = _gainTrack.Value / 10m; };
            pnl.Controls.Add(_gainTrack);
            y += 30;

            pnl.Controls.Add(MakeSectionLabel("采集模式", ref y));
            _continuousBtn = new Button
            {
                Text = "连续采集 (关闭)",
                Location = new Point(6, y),
                Size = new Size(pnl.Width - 18, 34),
                BackColor = Color.FromArgb(200, 100, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9f)
            };
            _continuousBtn.FlatAppearance.BorderSize = 0;
            _continuousBtn.Click += (s, e) => ToggleContinuous();
            pnl.Controls.Add(_continuousBtn);
            y += 42;

            var singleCapBtn = new Button
            {
                Text = "单张采集",
                Location = new Point(6, y),
                Size = new Size((pnl.Width - 24) / 2, 30),
                BackColor = Color.FromArgb(0, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9f)
            };
            singleCapBtn.FlatAppearance.BorderSize = 0;
            singleCapBtn.Click += (s, e) => CaptureSingle();
            pnl.Controls.Add(singleCapBtn);

            var saveBtn = new Button
            {
                Text = "保存截图",
                Location = new Point(12 + (pnl.Width - 24) / 2, y),
                Size = new Size((pnl.Width - 24) / 2, 30),
                BackColor = Color.FromArgb(80, 80, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9f)
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += (s, e) => SaveScreenshot();
            pnl.Controls.Add(saveBtn);
            y += 38;

            pnl.Controls.Add(MakeSectionLabel("ROI 区域", ref y));
            y = AddKvRow(pnl, "起点:", "-", y);
            y = AddKvRow(pnl, "终点:", "-", y);
            y = AddKvRow(pnl, "尺寸:", "-", y);

            var clearRoiBtn = new Button
            {
                Text = "清除 ROI",
                Location = new Point(6, y),
                Size = new Size(pnl.Width - 18, 28),
                BackColor = Color.FromArgb(180, 180, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9f)
            };
            clearRoiBtn.FlatAppearance.BorderSize = 0;
            clearRoiBtn.Click += (s, e) => { _roiRect = Rectangle.Empty; _roiLabel.Text = "ROI: -"; _pictureBox.Invalidate(); };
            pnl.Controls.Add(clearRoiBtn);
        }

        private void PopulateLayout()
        {
            // 占位，实际布局在 BuildLayout 中已完成
        }

        private Control MakeSectionLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(2, y),
                Size = new Size(190, 20),
                Font = new Font("Microsoft YaHei", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };
            y += 22;
            return lbl;
        }

        private int AddKvRow(Panel pnl, string key, string value, int y)
        {
            pnl.Controls.Add(new Label
            {
                Text = key,
                Location = new Point(6, y),
                Size = new Size(36, 18),
                Font = new Font("Microsoft YaHei", 8f),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            });
            pnl.Controls.Add(new Label
            {
                Text = value,
                Location = new Point(44, y),
                Size = new Size(140, 18),
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.FromArgb(30, 30, 50),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return y + 20;
        }

        private void RebuildCameraList()
        {
            // 清除旧项（保留标题）
            while (_cameraListPanel.Controls.Count > 1)
                _cameraListPanel.Controls.RemoveAt(1);

            int y = 36;
            foreach (var cam in _cameras)
            {
                var row = new Panel
                {
                    Location = new Point(4, y),
                    Size = new Size(_cameraListPanel.Width - 14, 34),
                    BackColor = Color.FromArgb(245, 248, 252),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = cam
                };

                var dot = new Label
                {
                    Location = new Point(6, 9),
                    Size = new Size(12, 12),
                    BackColor = cam.IsConnected ? Color.FromArgb(0, 180, 80) : Color.FromArgb(200, 60, 60),
                    Text = ""
                };
                row.Controls.Add(dot);

                row.Controls.Add(new Label
                {
                    Text = cam.Name,
                    Location = new Point(22, 7),
                    Size = new Size(row.Width - 60, 20),
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = cam.IsConnected ? Color.FromArgb(0, 100, 40) : Color.FromArgb(120, 40, 40)
                });

                row.DoubleClick += (s, e) => ToggleCameraConnection();
                _cameraListPanel.Controls.Add(row);
                y += 40;
            }
        }

        // ===== 相机操作 =====

        private void ToggleCameraConnection()
        {
            var cam = _cameras.Count > 0 ? _cameras[0] : null;
            if (cam == null) return;

            if (cam.IsConnected)
            {
                DisconnectCamera(cam);
                RebuildCameraList();
                UpdateRightPanelButton(false);
            }
            else
            {
                UpdateRightPanelButton(true); // 直接切到"关闭"状态
                _statusLabel.Text = "正在连接...";
                _statusLabel.ForeColor = Color.FromArgb(255, 200, 50);
                ConnectCameraAsync(cam);
            }
        }

        private async void ConnectCameraAsync(CameraInfo cam)
        {
            var boot = AppBootstrap.Instance;
            var hw = boot?.HardwareService;

            await Task.Run(async () =>
            {
                try
                {
                    if (hw != null)
                    {
                        var vision = hw.GetVisionSystem(cam.Id);
                        if (vision != null)
                        {
                            var connected = await vision.ConnectAsync(CancellationToken.None);
                            if (connected)
                            {
                                var info = vision.GetCameraInfo();
                                BeginInvoke((Action)(() =>
                                {
                                    cam.IsConnected = true;
                                    cam.LastError = null;
                                    cam.FrameCount = 0;
                                    var model = !string.IsNullOrEmpty(info?.Model) ? info.Model : vision.CameraModel;
                                    _statusLabel.Text = $"已连接 {cam.Name} ({model ?? "Camera"})";
                                    _statusLabel.ForeColor = Color.FromArgb(120, 255, 120);
                                    RebuildCameraList();
                                }));
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    cam.LastError = ex.Message;
                }
                finally
                {
                    if (!cam.IsConnected)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            // 硬件连接失败，使用模拟
                            cam.IsConnected = true;
                            cam.LastError = null;
                            cam.FrameCount = 0;
                            _statusLabel.Text = $"已连接 {cam.Name} (模拟)";
                            _statusLabel.ForeColor = Color.FromArgb(120, 255, 120);
                            RebuildCameraList();
                        }));
                    }
                }
            });
        }

        private void DisconnectCamera(CameraInfo cam)
        {
            StopContinuous();

            var boot = AppBootstrap.Instance;
            var vision = boot?.HardwareService?.GetVisionSystem(cam.Id);
            if (vision != null)
            {
                Task.Run(async () =>
                {
                    try { await vision.DisconnectAsync(); }
                    catch { }
                });
            }

            cam.IsConnected = false;
            _statusLabel.Text = "未连接";
            _statusLabel.ForeColor = Color.FromArgb(180, 180, 180);
            lock (_imageLock) { _currentImage?.Dispose(); _currentImage = null; }
            _imagePlaceholder.Visible = true;
            _imagePlaceholder.BringToFront();
            _pictureBox.Image = null;
            _fpsLabel.Text = "FPS: -";
        }

        private void UpdateRightPanelButton(bool connected)
        {
            // 遍历右侧面板找到"打开相机"按钮
            var rightPnl = (Panel)((TableLayoutPanel)Controls[0]).GetControlFromPosition(2, 0);
            foreach (Control c in rightPnl.Controls)
            {
                if (c is Button b && (b.Text.Contains("打开") || b.Text.Contains("关闭")))
                {
                    b.Text = connected ? "关闭相机" : "打开相机";
                    b.BackColor = connected ? Color.FromArgb(200, 50, 50) : Color.FromArgb(0, 140, 60);
                    break;
                }
            }
        }

        private void CaptureSingle()
        {
            var cam = _cameras.Count > 0 ? _cameras[0] : null;
            if (cam == null || !cam.IsConnected) return;

            var vision = AppBootstrap.Instance?.HardwareService?.GetVisionSystem(cam.Id);

            Task.Run(async () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool realCapture = false;

                try
                {
                    if (vision != null && vision.IsConnected)
                    {
                        byte[] data = await vision.CaptureAsync(CancellationToken.None);
                        if (data != null && data.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(data))
                            {
                                lock (_imageLock)
                                {
                                    _currentImage?.Dispose();
                                    _currentImage = new Bitmap(ms);
                                }
                            }
                            realCapture = true;
                        }
                    }
                }
                catch { }

                if (!realCapture)
                {
                    SimulateCaptureImage();
                }

                sw.Stop();
                cam.LastCaptureMs = sw.Elapsed.TotalMilliseconds;
                cam.FrameCount++;

                BeginInvoke((Action)(() =>
                {
                    _totalCaptures++;
                    _totalCaptureMs += cam.LastCaptureMs;
                    _statsLabel.Text = $"采集: {_totalCaptures} 次";
                    _fpsLabel.Text = $"耗时: {cam.LastCaptureMs:F0}ms";
                    _imagePlaceholder.Visible = false;
                    _pictureBox.Image?.Dispose();
                    lock (_imageLock)
                    {
                        _pictureBox.Image = _currentImage != null ? (Bitmap)_currentImage.Clone() : null;
                    }
                    _pictureBox.Invalidate();
                    RefreshCameraListInfo();
                }));
            });
        }

        private void SimulateCaptureImage()
        {
            int w = 640, h = 480;
            lock (_imageLock)
            {
                _currentImage?.Dispose();
                _currentImage = new Bitmap(w, h, PixelFormat.Format24bppRgb);

                using (var g = Graphics.FromImage(_currentImage))
                {
                    g.Clear(Color.FromArgb(20, 22, 28));

                    int cx = w / 2 + _rng.Next(-20, 20);
                    int cy = h / 2 + _rng.Next(-15, 15);
                    int r = 120 + _rng.Next(-5, 5);
                    using (var brush = new SolidBrush(Color.FromArgb(60, 65, 75)))
                        g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
                    using (var pen = new Pen(Color.FromArgb(140, 145, 155), 2))
                        g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);

                    using (var pen = new Pen(Color.FromArgb(0, 220, 80), 1))
                    {
                        g.DrawLine(pen, w / 2, 0, w / 2, h);
                        g.DrawLine(pen, 0, h / 2, w, h / 2);
                    }

                    for (int i = 0; i < 200; i++)
                    {
                        int nx = _rng.Next(w), ny = _rng.Next(h);
                        int noise = _rng.Next(0, 40);
                        _currentImage.SetPixel(nx, ny, Color.FromArgb(noise, noise, noise + 5));
                    }

                    g.DrawString(DateTime.Now.ToString("HH:mm:ss.fff"),
                        new Font("Consolas", 10), Brushes.Lime, 5, 5);
                }
            }
        }

        private void ToggleContinuous()
        {
            if (_isContinuous)
                StopContinuous();
            else
                StartContinuous();
        }

        private void StartContinuous()
        {
            var cam = _cameras.Count > 0 ? _cameras[0] : null;
            if (cam == null || !cam.IsConnected) return;

            var vision = AppBootstrap.Instance?.HardwareService?.GetVisionSystem(cam.Id);

            _isContinuous = true;
            _continuousCts = new CancellationTokenSource();
            _continuousBtn.Text = "连续采集 (运行中...)";
            _continuousBtn.BackColor = Color.FromArgb(50, 180, 50);

            var ct = _continuousCts.Token;
            Task.Run(async () =>
            {
                DateTime lastFpsUpdate = DateTime.Now;
                int fpsCounter = 0;
                bool useReal = vision != null && vision.IsConnected;

                while (!ct.IsCancellationRequested)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    if (useReal)
                    {
                        try
                        {
                            byte[] data = await vision.CaptureAsync(ct);
                            if (data != null && data.Length > 0)
                            {
                                using (var ms = new System.IO.MemoryStream(data))
                                {
                                    lock (_imageLock)
                                    {
                                        _currentImage?.Dispose();
                                        _currentImage = new Bitmap(ms);
                                    }
                                }
                                BeginInvoke((Action)(() =>
                                {
                                    _pictureBox.Image?.Dispose();
                                    lock (_imageLock)
                                    {
                                        _pictureBox.Image = _currentImage != null ? (Bitmap)_currentImage.Clone() : null;
                                    }
                                    _pictureBox.Invalidate();
                                    _imagePlaceholder.Visible = false;
                                }));
                            }
                        }
                        catch { SimulateCaptureImage(); }
                    }
                    else
                    {
                        SimulateCaptureImage();
                    }

                    sw.Stop();
                    cam.LastCaptureMs = sw.Elapsed.TotalMilliseconds;
                    cam.FrameCount++;
                    fpsCounter++;
                    _totalCaptures++;
                    _totalCaptureMs += cam.LastCaptureMs;

                    var now = DateTime.Now;
                    var elapsed = (now - lastFpsUpdate).TotalSeconds;
                    if (elapsed >= 1.0)
                    {
                        double fps = fpsCounter / elapsed;
                        BeginInvoke((Action)(() =>
                        {
                            _fpsLabel.Text = $"FPS: {fps:F1}";
                            _statsLabel.Text = $"采集: {_totalCaptures} 次";
                            RefreshCameraListInfo();
                        }));
                        fpsCounter = 0;
                        lastFpsUpdate = now;
                    }

                    int interval = Math.Max(50, (int)(_exposureNum.Value / 1000m + 30));
                    try { await Task.Delay(interval, ct); }
                    catch (TaskCanceledException) { break; }
                }
            }, ct);
        }

        private void StopContinuous()
        {
            _isContinuous = false;
            _continuousCts?.Cancel();
            _continuousCts?.Dispose();
            _continuousCts = null;
            _continuousBtn.Text = "连续采集 (关闭)";
            _continuousBtn.BackColor = Color.FromArgb(200, 100, 20);
        }

        private void RefreshCameraListInfo()
        {
            foreach (Control c in _cameraListPanel.Controls)
            {
                if (c is Panel row && row.Tag is CameraInfo cam)
                {
                    foreach (Control child in row.Controls)
                    {
                        if (child is Label lbl && lbl.Text == cam.Name)
                        {
                            lbl.Text = cam.IsConnected
                                ? $"{cam.Name}  [{cam.FrameCount}帧]"
                                : cam.Name;
                            lbl.ForeColor = cam.IsConnected
                                ? Color.FromArgb(0, 100, 40)
                                : Color.FromArgb(120, 40, 40);
                        }
                        if (child is Label dot && dot.Size.Width < 15)
                        {
                            dot.BackColor = cam.IsConnected
                                ? Color.FromArgb(0, 180, 80)
                                : Color.FromArgb(200, 60, 60);
                        }
                    }
                }
            }
        }

        private void SaveScreenshot()
        {
            lock (_imageLock)
            {
                if (_currentImage == null) return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|Bitmap|*.bmp|JPEG|*.jpg",
                FileName = $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    lock (_imageLock)
                    {
                        _currentImage?.Save(sfd.FileName);
                    }
                }
            }
        }

        // ===== ROI 绘制 =====

        private void OnImagePaint(object sender, PaintEventArgs e)
        {
            if (_currentImage == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 画 ROI
            if (_roiRect != Rectangle.Empty && _roiRect.Width > 5 && _roiRect.Height > 5)
            {
                // 转换为图像坐标下的 ROI 框
                var imgRect = GetImageDisplayRect();
                if (imgRect.Width > 0 && imgRect.Height > 0)
                {
                    float scaleX = (float)imgRect.Width / _pictureBox.ClientSize.Width;
                    float scaleY = (float)imgRect.Height / _pictureBox.ClientSize.Height;

                    // 如果缩放模式是 Zoom，需要考虑居中偏移
                    float zoomScale = Math.Min((float)_pictureBox.ClientSize.Width / _currentImage.Width,
                                               (float)_pictureBox.ClientSize.Height / _currentImage.Height);
                    int zoomW = (int)(_currentImage.Width * zoomScale);
                    int zoomH = (int)(_currentImage.Height * zoomScale);
                    int offsetX = (_pictureBox.ClientSize.Width - zoomW) / 2;
                    int offsetY = (_pictureBox.ClientSize.Height - zoomH) / 2;

                    // ROI 在屏幕上的位置
                    int rx = (int)(_roiRect.X * zoomScale) + offsetX;
                    int ry = (int)(_roiRect.Y * zoomScale) + offsetY;
                    int rw = (int)(_roiRect.Width * zoomScale);
                    int rh = (int)(_roiRect.Height * zoomScale);

                    using (var pen = new Pen(Color.FromArgb(0, 255, 100), 2))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(pen, rx, ry, rw, rh);
                    }
                    using (var brush = new SolidBrush(Color.FromArgb(30, 0, 255, 0)))
                        g.FillRectangle(brush, rx, ry, rw, rh);

                    // 拖动中的框
                    if (_isDraggingRoi)
                    {
                        int dx = Math.Min(_roiStart.X, _roiEnd.X);
                        int dy = Math.Min(_roiStart.Y, _roiEnd.Y);
                        int dw = Math.Abs(_roiEnd.X - _roiStart.X);
                        int dh = Math.Abs(_roiEnd.Y - _roiStart.Y);
                        using (var pen = new Pen(Color.Yellow, 1.5f))
                            g.DrawRectangle(pen, dx, dy, dw, dh);
                    }
                }
            }
        }

        private Rectangle GetImageDisplayRect()
        {
            if (_currentImage == null) return Rectangle.Empty;

            float scale = Math.Min((float)_pictureBox.ClientSize.Width / _currentImage.Width,
                                   (float)_pictureBox.ClientSize.Height / _currentImage.Height);
            int w = (int)(_currentImage.Width * scale);
            int h = (int)(_currentImage.Height * scale);
            int x = (_pictureBox.ClientSize.Width - w) / 2;
            int y = (_pictureBox.ClientSize.Height - h) / 2;
            return new Rectangle(x, y, w, h);
        }

        private void OnImageMouseDown(object sender, MouseEventArgs e)
        {
            if (_currentImage == null) return;
            _isDraggingRoi = true;
            _roiStart = e.Location;
            _roiEnd = e.Location;
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingRoi) return;
            _roiEnd = e.Location;
            _pictureBox.Invalidate();

            int x = Math.Min(_roiStart.X, _roiEnd.X);
            int y = Math.Min(_roiStart.Y, _roiEnd.Y);
            int w = Math.Abs(_roiEnd.X - _roiStart.X);
            int h = Math.Abs(_roiEnd.Y - _roiStart.Y);

            // 转为图像坐标
            float zoomScale = Math.Min((float)_pictureBox.ClientSize.Width / _currentImage.Width,
                                       (float)_pictureBox.ClientSize.Height / _currentImage.Height);
            int zoomW = (int)(_currentImage.Width * zoomScale);
            int zoomH = (int)(_currentImage.Height * zoomScale);
            int offsetX = (_pictureBox.ClientSize.Width - zoomW) / 2;
            int offsetY = (_pictureBox.ClientSize.Height - zoomH) / 2;

            int ix = (int)((x - offsetX) / zoomScale);
            int iy = (int)((y - offsetY) / zoomScale);
            int iw = (int)(w / zoomScale);
            int ih = (int)(h / zoomScale);

            _roiLabel.Text = $"ROI: ({ix},{iy}) {iw}x{ih}";
        }

        private void OnImageMouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDraggingRoi) return;
            _isDraggingRoi = false;
            _roiEnd = e.Location;

            int x = Math.Min(_roiStart.X, _roiEnd.X);
            int y = Math.Min(_roiStart.Y, _roiEnd.Y);
            int w = Math.Abs(_roiEnd.X - _roiStart.X);
            int h = Math.Abs(_roiEnd.Y - _roiStart.Y);

            float zoomScale = Math.Min((float)_pictureBox.ClientSize.Width / _currentImage.Width,
                                       (float)_pictureBox.ClientSize.Height / _currentImage.Height);
            int zoomW = (int)(_currentImage.Width * zoomScale);
            int zoomH = (int)(_currentImage.Height * zoomScale);
            int offsetX = (_pictureBox.ClientSize.Width - zoomW) / 2;
            int offsetY = (_pictureBox.ClientSize.Height - zoomH) / 2;

            _roiRect = new Rectangle(
                (int)((x - offsetX) / zoomScale),
                (int)((y - offsetY) / zoomScale),
                (int)(w / zoomScale),
                (int)(h / zoomScale)
            );

            _roiLabel.Text = $"ROI: ({_roiRect.X},{_roiRect.Y}) {_roiRect.Width}x{_roiRect.Height}";
            _pictureBox.Invalidate();
        }

        // ===== IModuleView =====

        public void OnActivated()
        {
            RebuildCameraList();
        }

        public void OnDeactivated()
        {
            StopContinuous();
        }

        public void RefreshData()
        {
            RebuildCameraList();
            RefreshCameraListInfo();
        }
    }
}