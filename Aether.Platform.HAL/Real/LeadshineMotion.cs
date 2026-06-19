namespace Aether.Platform.HAL.Real
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class LeadshineMotion : IAxis
    {
        public string AxisId { get; set; }
        public bool IsEnabled { get; private set; }
        public bool IsMoving { get; private set; }
        public double CurrentPosition { get; private set; }
        public bool IsHomed { get; private set; }
        public Task MoveAbsAsync(double position, double speed, CancellationToken ct) => Task.CompletedTask;
        public Task MoveRelAsync(double distance, double speed, CancellationToken ct) => Task.CompletedTask;
        public Task HomeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
        public Task EnableAsync(CancellationToken ct) { IsEnabled = true; return Task.CompletedTask; }
        public Task DisableAsync(CancellationToken ct) { IsEnabled = false; return Task.CompletedTask; }
    }
}
