namespace Aether.Platform.Workflow
{
    /// <summary>工作流节点类型</summary>
    public enum WorkflowNodeType
    {
        Start,
        End,
        Sequence,
        Parallel,
        Condition,
        Loop,
        Wait,
        Delay,
        AxisMove,
        DioWrite,
        PlcWrite,
        VisionCapture,
        ScannerRead,
        LuaScript,
        Log,
        SetVariable,
        IfCondition,
    }
}
