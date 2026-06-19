using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Aether.Platform.Workflow
{
    /// <summary>工作流中的单个节点</summary>
    public class WorkflowNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public WorkflowNodeType Type { get; set; }
        public List<string> NextNodeIds { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // 画布位置
        public Point Position { get; set; }
        public Size Size { get; set; } = new Size(120, 50);

        // 运行时状态
        public NodeResult Status { get; set; } = NodeResult.Skipped;
        public string ErrorMessage { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public override string ToString() => $"[{Type}] {Name} (Id={Id})";

        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (Properties.TryGetValue(key, out var val) && val is T t)
                return t;
            return defaultValue;
        }

        /// <summary>是否为流程控制类节点（不依赖硬件）</summary>
        public bool IsFlowControl => Type == WorkflowNodeType.Start ||
            Type == WorkflowNodeType.End ||
            Type == WorkflowNodeType.Sequence ||
            Type == WorkflowNodeType.Parallel ||
            Type == WorkflowNodeType.Condition ||
            Type == WorkflowNodeType.Loop ||
            Type == WorkflowNodeType.IfCondition;

        /// <summary>是否为硬件操作类节点</summary>
        public bool IsHardwareOp => Type == WorkflowNodeType.AxisMove ||
            Type == WorkflowNodeType.DioWrite ||
            Type == WorkflowNodeType.PlcWrite ||
            Type == WorkflowNodeType.VisionCapture ||
            Type == WorkflowNodeType.ScannerRead;

        /// <summary>获取下一个节点 ID（不含条件分支）</summary>
        public string NextNodeId => NextNodeIds.Count > 0 ? NextNodeIds[0] : null;
    }
}
