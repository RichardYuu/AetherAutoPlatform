using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Aether.Platform.Workflow
{
    /// <summary>工作流 Canvas 画布控件</summary>
    public class WorkflowCanvas : UserControl
    {
        private WorkflowDefinition _workflow;
        private Point _dragOffset;
        private WorkflowNode _draggingNode;
        private bool _isDragging;
        private WorkflowNode _linkingSource;    // Ctrl+点击连线源节点
        private Point _linkCursorPos;           // 连线橡皮筋光标位置
        private float _zoomFactor = 1.0f;       // 缩放比例 (0.5x ~ 2.0x)
        private Point _panOffset = Point.Empty; // 平移偏移
        private bool _isPanning;                // 中键拖拽平移
        private Point _panStart;

        private static readonly Color CanvasBg = Color.FromArgb(245, 246, 248);
        private static readonly Color NodeBg = Color.FromArgb(255, 255, 255);
        private static readonly Color NodeBorder = Color.FromArgb(180, 185, 195);
        private static readonly Color NodeSelectedBorder = Color.FromArgb(0, 120, 215);
        private static readonly Color StartBg = Color.FromArgb(39, 174, 96);
        private static readonly Color EndBg = Color.FromArgb(231, 76, 60);
        private static readonly Color FlowBg = Color.FromArgb(52, 152, 219);
        private static readonly Color HardwareBg = Color.FromArgb(243, 156, 18);
        private static readonly Color ScriptBg = Color.FromArgb(155, 89, 182);
        private static readonly Color LineColor = Color.FromArgb(140, 150, 165);
        private static readonly Color TextColor = Color.FromArgb(40, 40, 45);
        private static readonly Color LinkPreviewColor = Color.FromArgb(0, 200, 83);

        public WorkflowNode SelectedNode { get; private set; }
        public WorkflowDefinition Definition => _workflow;
        public event Action<WorkflowNode> OnNodeSelected;
        public event Action<WorkflowNode> OnNodeDoubleClicked;
        public event Action<NodeConnection> OnConnectionCreated;

        public WorkflowCanvas()
        {
            BackColor = CanvasBg;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            Paint += OnPaint;
            KeyDown += OnKeyDown;
            MouseWheel += OnMouseWheel;
        }

        public void LoadWorkflow(WorkflowDefinition workflow)
        {
            _workflow = workflow;
            AutoArrange();
            Invalidate();
        }

        /// <summary>自动排列节点（简单网格布局）</summary>
        public void AutoArrange()
        {
            if (_workflow?.Nodes == null || _workflow.Nodes.Count == 0) return;

            int cols = Math.Max(1, (int)Math.Sqrt(_workflow.Nodes.Count));
            int x = 20, y = 20;
            int idx = 0;

            foreach (var node in _workflow.Nodes.OrderBy(n => n.Type == WorkflowNodeType.Start ? 0 : 1)
                                               .ThenBy(n => n.Id))
            {
                node.Position = new Point(x + (idx % cols) * 160, y + (idx / cols) * 80);
                idx++;
            }
            Invalidate();
        }

        public void Clear() { _workflow = null; Invalidate(); }

        public void ResetView()
        {
            _zoomFactor = 1.0f;
            _panOffset = Point.Empty;
            Invalidate();
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (_workflow == null) return;

            float delta = e.Delta > 0 ? 0.1f : -0.1f;
            _zoomFactor = Math.Max(0.3f, Math.Min(3.0f, _zoomFactor + delta));

            // 以鼠标位置为中心缩放
            float oldZoom = _zoomFactor - delta;
            if (oldZoom > 0)
            {
                float ratio = _zoomFactor / oldZoom - 1f;
                _panOffset = new Point(
                    (int)(_panOffset.X - e.X * ratio),
                    (int)(_panOffset.Y - e.Y * ratio));
            }

            Invalidate();
        }

        // ---- 绘制 ----

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (_workflow == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 缩放 + 平移变换
            g.ScaleTransform(_zoomFactor, _zoomFactor);
            g.TranslateTransform(_panOffset.X, _panOffset.Y);

            // 先画连线
            foreach (var conn in _workflow.Connections)
            {
                var from = _workflow.GetNode(conn.FromNodeId);
                var to = _workflow.GetNode(conn.ToNodeId);
                if (from == null || to == null) continue;
                DrawConnection(g, from, to, conn.Label);
            }

            // 再画节点
            foreach (var node in _workflow.Nodes)
                DrawNode(g, node);

            // 连线模式橡皮筋预览
            if (_linkingSource != null)
            {
                int sx = _linkingSource.Position.X + _linkingSource.Size.Width / 2;
                int sy = _linkingSource.Position.Y + _linkingSource.Size.Height;

                // linkCursorPos 是屏幕坐标，转换为世界坐标
                float invZoom = 1.0f / _zoomFactor;
                int tx = (int)(_linkCursorPos.X * invZoom - _panOffset.X);
                int ty = (int)(_linkCursorPos.Y * invZoom - _panOffset.Y);

                using (var pen = new Pen(LinkPreviewColor, 2f) { DashStyle = DashStyle.Dash })
                {
                    int midY = (sy + ty) / 2;
                    g.DrawBezier(pen, sx, sy, sx, midY, tx, midY, tx, ty);
                }
                // 目标高亮
                var hitTarget = HitTest(_linkCursorPos);
                if (hitTarget != null && hitTarget != _linkingSource)
                {
                    var hitRect = new Rectangle(hitTarget.Position, hitTarget.Size);
                    using (var pen = new Pen(LinkPreviewColor, 3f))
                    {
                        g.DrawRectangle(pen, hitRect);
                    }
                }
            }
        }

        private void DrawNode(Graphics g, WorkflowNode node)
        {
            var rect = new Rectangle(node.Position, node.Size);
            bool isSelected = SelectedNode == node;
            bool isRunning = node.Status == NodeResult.Running;

            Color bg;
            if (node.Type == WorkflowNodeType.Start) bg = StartBg;
            else if (node.Type == WorkflowNodeType.End) bg = EndBg;
            else if (node.Type >= WorkflowNodeType.AxisMove && node.Type <= WorkflowNodeType.ScannerRead) bg = HardwareBg;
            else if (node.Type == WorkflowNodeType.LuaScript) bg = ScriptBg;
            else bg = NodeBg;
            Color border = isSelected ? NodeSelectedBorder : NodeBorder;
            Color text = (node.Type == WorkflowNodeType.Start || node.Type == WorkflowNodeType.End) ? Color.White : TextColor;

            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border, isSelected ? 2f : 1f))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }

            // 节点名
            using (var font = new Font("Microsoft YaHei", 9f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(text))
            {
                var textRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, 18);
                g.DrawString(node.Name, font, textBrush, textRect,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // 类型标签
            using (var font = new Font("Microsoft YaHei", 7f))
            using (var textBrush = new SolidBrush(Color.FromArgb(120, 120, 130)))
            {
                var typeRect = new Rectangle(rect.X + 4, rect.Y + 24, rect.Width - 8, 18);
                g.DrawString(node.Type.ToString(), font, textBrush, typeRect,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // 执行状态指示
            if (isRunning)
            {
                using (var pen = new Pen(Color.FromArgb(39, 174, 96), 2f))
                    g.DrawRectangle(pen, rect);
            }
            if (node.Status == NodeResult.Failure)
            {
                using (var pen = new Pen(Color.FromArgb(231, 76, 60), 2f))
                    g.DrawRectangle(pen, rect);
            }
        }

        private void DrawConnection(Graphics g, WorkflowNode from, WorkflowNode to, string label)
        {
            int x1 = from.Position.X + from.Size.Width / 2;
            int y1 = from.Position.Y + from.Size.Height;
            int x2 = to.Position.X + to.Size.Width / 2;
            int y2 = to.Position.Y;

            using (var pen = new Pen(LineColor, 1.5f)
            {
                EndCap = LineCap.ArrowAnchor
            })
            {
                int midY = (y1 + y2) / 2;
                g.DrawBezier(pen, x1, y1, x1, midY, x2, midY, x2, y2);
            }

            if (!string.IsNullOrEmpty(label))
            {
                using (var font = new Font("Microsoft YaHei", 7f))
                using (var brush = new SolidBrush(Color.FromArgb(100, 105, 115)))
                    g.DrawString(label, font, brush, (x1 + x2) / 2 - 15, (y1 + y2) / 2 - 10);
            }
        }

        // ---- 拖拽 ----

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_workflow == null) return;

            // 中键：平移模式
            if (e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _panStart = e.Location;
                _isDragging = false;
                _linkingSource = null;
                this.Cursor = Cursors.SizeAll;
                return;
            }

            var hit = HitTest(e.Location);

            // Ctrl+点击：连线模式
            if (Control.ModifierKeys == Keys.Control && hit != null)
            {
                if (_linkingSource == null)
                {
                    // 开始连线
                    _linkingSource = hit;
                    _linkCursorPos = e.Location;
                    Invalidate();
                }
                else if (hit != _linkingSource)
                {
                    // 完成连线
                    CreateConnection(_linkingSource, hit);
                    _linkingSource = null;
                    Invalidate();
                }
                else
                {
                    // 点击自己 = 取消
                    _linkingSource = null;
                    Invalidate();
                }
                return;
            }

            // 普通模式：取消连线
            if (_linkingSource != null)
            {
                _linkingSource = null;
                Invalidate();
            }

            if (hit != null)
            {
                _draggingNode = hit;
                _dragOffset = new Point(e.X - hit.Position.X, e.Y - hit.Position.Y);
                _isDragging = true;
                SelectNode(hit);
            }
            else
            {
                SelectedNode = null;
                Invalidate();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                int dx = e.X - _panStart.X;
                int dy = e.Y - _panStart.Y;
                _panOffset = new Point(_panOffset.X + dx, _panOffset.Y + dy);
                _panStart = e.Location;
                Invalidate();
                return;
            }

            if (_linkingSource != null)
            {
                _linkCursorPos = e.Location;
                Invalidate();
                return;
            }

            if (!_isDragging || _draggingNode == null) return;
            _draggingNode.Position = new Point(e.X - _dragOffset.X, e.Y - _dragOffset.Y);
            Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                this.Cursor = Cursors.Default;
                return;
            }
            _isDragging = false;
            _draggingNode = null;
        }

        private WorkflowNode HitTest(Point pt)
        {
            if (_workflow == null) return null;

            // 屏幕坐标 → 世界坐标（逆变换）
            float invZoom = 1.0f / _zoomFactor;
            int wx = (int)(pt.X * invZoom - _panOffset.X);
            int wy = (int)(pt.Y * invZoom - _panOffset.Y);
            var worldPt = new Point(wx, wy);

            foreach (var node in _workflow.Nodes)
            {
                var rect = new Rectangle(node.Position, node.Size);
                if (rect.Contains(worldPt)) return node;
            }
            return null;
        }

        private void SelectNode(WorkflowNode node)
        {
            if (SelectedNode == node) return;
            SelectedNode = node;
            OnNodeSelected?.Invoke(node);
            Invalidate();
        }

        private void CreateConnection(WorkflowNode from, WorkflowNode to)
        {
            if (_workflow == null || from == null || to == null) return;
            if (from == to) return;

            // 检查是否已存在连接
            bool exists = _workflow.Connections.Any(c =>
                c.FromNodeId == from.Id && c.ToNodeId == to.Id);
            if (exists) return;

            var conn = new NodeConnection
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                FromNodeId = from.Id,
                ToNodeId = to.Id,
                Label = ""
            };
            _workflow.Connections.Add(conn);
            OnConnectionCreated?.Invoke(conn);
            Invalidate();
        }

        public void RemoveConnection(NodeConnection conn)
        {
            if (_workflow == null || conn == null) return;
            _workflow.Connections.Remove(conn);
            Invalidate();
        }

        public void RemoveSelectedConnection()
        {
            if (_workflow == null || SelectedNode == null) return;

            var conns = _workflow.Connections.Where(c =>
                c.FromNodeId == SelectedNode.Id || c.ToNodeId == SelectedNode.Id).ToList();

            foreach (var conn in conns)
                _workflow.Connections.Remove(conn);

            Invalidate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // 取消连线模式
                if (_linkingSource != null)
                {
                    _linkingSource = null;
                    Invalidate();
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                // 删除选中节点的所有连接
                if (SelectedNode != null)
                {
                    RemoveSelectedConnection();
                    e.Handled = true;
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            var hit = HitTest(e.Location);
            if (hit != null)
                OnNodeDoubleClicked?.Invoke(hit);
        }
    }
}
