using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Workflow
{
    /// <summary>硬件执行委托 — 由外部注入，实现与 HAL 层解耦</summary>
    public class WorkflowExecutionHooks
    {
        public Func<string, double, double, CancellationToken, Task<bool>> AxisMoveAsync { get; set; }
        public Func<string, double, CancellationToken, Task<double>> AxisGetPosAsync { get; set; }
        public Func<int, bool, CancellationToken, Task<bool>> DioWriteAsync { get; set; }
        public Func<string, object, CancellationToken, Task<bool>> PlcWriteAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> VisionCaptureAsync { get; set; }
        public Func<CancellationToken, Task<string>> ScannerReadAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> LuaExecuteAsync { get; set; }
        public Action<string, string> LogCallback { get; set; }
    }
}
