using System;
using System.Collections.Generic;
using System.Linq;

namespace Aether.Platform.Workflow
{
    /// <summary>工作流构建器 — 流式 API</summary>
    public class WorkflowBuilder
    {
        private readonly WorkflowDefinition _def = new WorkflowDefinition();
        private string _lastNodeId;
        private int _nodeCounter;

        public WorkflowBuilder(string id, string name, string deviceType = null)
        {
            _def.Id = id;
            _def.Name = name;
            _def.DeviceType = deviceType;
        }

        public WorkflowDefinition Build()
        {
            _def.RebuildConnections();
            return _def;
        }

        /// <summary>添加起始节点</summary>
        public WorkflowBuilder Start(string name = "开始")
        {
            return AddNode(WorkflowNodeType.Start, name);
        }

        /// <summary>添加结束节点</summary>
        public WorkflowBuilder End(string name = "结束")
        {
            var node = AddNode(WorkflowNodeType.End, name);
            Connect(_lastNodeId, node._lastNodeId);
            return this;
        }

        /// <summary>添加延时节点</summary>
        public WorkflowBuilder Delay(int milliseconds, string name = null)
        {
            return AddNode(WorkflowNodeType.Delay, name ?? $"延时{milliseconds}ms",
                ("Milliseconds", milliseconds));
        }

        /// <summary>添加轴移动</summary>
        public WorkflowBuilder AxisMove(string axisId, double targetPos, double speed = 50)
        {
            return AddNode(WorkflowNodeType.AxisMove, $"轴{axisId}→{targetPos}",
                ("AxisId", axisId), ("TargetPosition", targetPos), ("Speed", speed));
        }

        /// <summary>添加 IO 写</summary>
        public WorkflowBuilder DioWrite(int ioIndex, bool state)
        {
            return AddNode(WorkflowNodeType.DioWrite, $"IO{ioIndex}={(state ? "ON" : "OFF")}",
                ("IoIndex", ioIndex), ("State", state));
        }

        /// <summary>添加等待条件</summary>
        public WorkflowBuilder WaitFor(string variableKey, object expectedValue, int timeoutMs = 30000)
        {
            return AddNode(WorkflowNodeType.Wait, $"等待 {variableKey}={expectedValue}",
                ("WaitVariable", variableKey), ("ExpectedValue", expectedValue), ("TimeoutMs", timeoutMs));
        }

        /// <summary>添加 Lua 脚本节点</summary>
        public WorkflowBuilder LuaScript(string script, string name = null)
        {
            return AddNode(WorkflowNodeType.LuaScript, name ?? "Lua",
                ("Script", script));
        }

        /// <summary>添加日志节点</summary>
        public WorkflowBuilder Log(string message)
        {
            return AddNode(WorkflowNodeType.Log, $"日志",
                ("Message", message));
        }

        /// <summary>添加视觉采集</summary>
        public WorkflowBuilder VisionCapture(string modelName = "default")
        {
            return AddNode(WorkflowNodeType.VisionCapture, $"视觉[{modelName}]",
                ("ModelName", modelName));
        }

        /// <summary>添加扫码</summary>
        public WorkflowBuilder ScannerRead()
        {
            return AddNode(WorkflowNodeType.ScannerRead, "扫码");
        }

        /// <summary>添加循环</summary>
        public WorkflowBuilder Loop(int maxIterations, string loopBodyId, string name = null)
        {
            return AddNode(WorkflowNodeType.Loop, name ?? $"循环×{maxIterations}",
                ("MaxIterations", maxIterations), ("LoopBodyNodeId", loopBodyId));
        }

        /// <summary>添加条件分支</summary>
        public WorkflowBuilder IfCondition(string variableKey, string op, string compareValue, string name = null)
        {
            return AddNode(WorkflowNodeType.IfCondition, name ?? $"如果 {variableKey}{op}{compareValue}",
                ("VariableKey", variableKey), ("Operator", op), ("CompareValue", compareValue));
        }

        /// <summary>设置变量</summary>
        public WorkflowBuilder SetVariable(string key, object value)
        {
            return AddNode(WorkflowNodeType.SetVariable, $"设置 {key}",
                ("Key", key), ("Value", value));
        }

        /// <summary>PLC 写</summary>
        public WorkflowBuilder PlcWrite(string address, object value)
        {
            return AddNode(WorkflowNodeType.PlcWrite, $"PLC {address}={value}",
                ("Address", address), ("Value", value));
        }

        // ---- 内部 ----

        private WorkflowBuilder AddNode(WorkflowNodeType type, string name, params (string, object)[] props)
        {
            string oldId = _lastNodeId;
            _lastNodeId = $"N{++_nodeCounter}";

            var node = new WorkflowNode
            {
                Id = _lastNodeId,
                Name = name,
                Type = type,
            };
            foreach (var (k, v) in props)
                node.Properties[k] = v;

            _def.Nodes.Add(node);

            if (oldId != null)
                Connect(oldId, _lastNodeId);

            return this;
        }

        /// <summary>手动连接两个节点</summary>
        public void Connect(string fromId, string toId)
        {
            if (_def.Connections.Any(c => c.FromNodeId == fromId && c.ToNodeId == toId))
                return;
            _def.Connections.Add(new NodeConnection { FromNodeId = fromId, ToNodeId = toId });
        }
    }
}
