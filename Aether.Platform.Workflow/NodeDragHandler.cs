using System;
using System.Collections.Generic;
using System.Drawing;

namespace Aether.Platform.Workflow
{
    /// <summary>独立拖拽逻辑（供外部复用）</summary>
    public class NodeDragHandler
    {
        private WorkflowNode _target;
        private Point _offset;
        private bool _isDragging;

        public bool IsDragging => _isDragging;
        public WorkflowNode Target => _target;

        public void StartDrag(WorkflowNode node, Point mousePt)
        {
            _target = node;
            _offset = new Point(mousePt.X - node.Position.X, mousePt.Y - node.Position.Y);
            _isDragging = true;
        }

        public void Drag(Point mousePt)
        {
            if (!_isDragging || _target == null) return;
            _target.Position = new Point(mousePt.X - _offset.X, mousePt.Y - _offset.Y);
        }

        public void EndDrag()
        {
            _isDragging = false;
            _target = null;
        }

        public WorkflowNode HitTest(IEnumerable<WorkflowNode> nodes, Point pt)
        {
            foreach (var node in nodes)
            {
                var rect = new Rectangle(node.Position, node.Size);
                if (rect.Contains(pt)) return node;
            }
            return null;
        }
    }
}
