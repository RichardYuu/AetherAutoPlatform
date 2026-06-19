using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Aether.Platform.Workflow
{
    /// <summary>工作流定义</summary>
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DeviceType { get; set; }
        public string Version { get; set; } = "1.0";
        public string Description { get; set; }
        public string FilePath { get; set; }
        public List<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
        public List<NodeConnection> Connections { get; set; } = new List<NodeConnection>();

        public WorkflowNode GetNode(string id) => Nodes.FirstOrDefault(n => n.Id == id);
        public WorkflowNode GetStartNode() => Nodes.FirstOrDefault(n => n.Type == WorkflowNodeType.Start);

        /// <summary>根据连接关系重建 NextNodeIds</summary>
        public void RebuildConnections()
        {
            foreach (var node in Nodes)
                node.NextNodeIds = Connections
                    .Where(c => c.FromNodeId == node.Id)
                    .Select(c => c.ToNodeId)
                    .ToList();
        }

        /// <summary>获取从某个节点出发的所有连接</summary>
        public List<NodeConnection> GetOutgoingConnections(string nodeId) =>
            Connections.Where(c => c.FromNodeId == nodeId).ToList();
    }
}
