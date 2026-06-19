using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Aether.Platform.Workflow
{
    /// <summary>节点间的连接线</summary>
    public class NodeConnection
    {
        public string Id { get; set; }
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public string Label { get; set; }
        public string ConditionExpression { get; set; }
    }
}
