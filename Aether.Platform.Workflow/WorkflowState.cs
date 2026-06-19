namespace Aether.Platform.Workflow
{
    /// <summary>工作流整体状态</summary>
    public enum WorkflowState
    {
        Idle,
        Running,
        Paused,
        Completed,
        Failed,
        Aborted,
    }
}
