using System;
using System.Drawing;

namespace Aether.Platform.Workflow
{
    /// <summary>节点样式渲染工具</summary>
    public class CanvasRenderer
    {
        public static readonly Color StartColor = Color.FromArgb(39, 174, 96);
        public static readonly Color EndColor = Color.FromArgb(231, 76, 60);
        public static readonly Color FlowColor = Color.FromArgb(52, 152, 219);
        public static readonly Color HardwareColor = Color.FromArgb(243, 156, 18);
        public static readonly Color ScriptColor = Color.FromArgb(155, 89, 182);
        public static readonly Color DefaultColor = Color.FromArgb(240, 240, 245);
        public static readonly Color ConnectionColor = Color.FromArgb(140, 150, 165);
        public static readonly Color SelectedColor = Color.FromArgb(0, 120, 215);

        public static Color GetNodeColor(WorkflowNodeType type)
        {
            if (type == WorkflowNodeType.Start) return StartColor;
            if (type == WorkflowNodeType.End) return EndColor;
            if (type == WorkflowNodeType.LuaScript) return ScriptColor;
            if (type >= WorkflowNodeType.AxisMove && type <= WorkflowNodeType.ScannerRead) return HardwareColor;
            return DefaultColor;
        }

        public static void DrawNode(Graphics g, WorkflowNode node, bool isSelected)
        {
            var rect = new Rectangle(node.Position, node.Size);
            var bg = GetNodeColor(node.Type);
            var borderColor = isSelected ? SelectedColor : Color.FromArgb(180, 185, 195);
            bool isDarkBg = node.Type == WorkflowNodeType.Start || node.Type == WorkflowNodeType.End;
            var textColor = isDarkBg ? Color.White : Color.FromArgb(40, 40, 45);

            using (var bgBrush = new SolidBrush(bg))
            using (var borderPen = new Pen(borderColor, isSelected ? 2f : 1f))
            {
                g.FillRectangle(bgBrush, rect);
                g.DrawRectangle(borderPen, rect);
            }

            using (var font = new Font("Microsoft YaHei", 9f, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                var textRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height / 2);
                g.DrawString(node.Name, font, brush, textRect, format);

                using (var smallFont = new Font("Microsoft YaHei", 7f))
                using (var grayBrush = new SolidBrush(isDarkBg ? Color.FromArgb(200, 220, 200) : Color.FromArgb(120, 120, 130)))
                {
                    var typeRect = new Rectangle(rect.X + 2, rect.Y + rect.Height / 2, rect.Width - 4, rect.Height / 2 - 4);
                    g.DrawString(node.Type.ToString(), smallFont, grayBrush, typeRect, format);
                }
            }
        }
    }
}
