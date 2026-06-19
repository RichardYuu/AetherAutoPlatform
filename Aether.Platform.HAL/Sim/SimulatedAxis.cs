namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedAxis : IAxis
    {
        public string AxisId { get; set; }
        public bool IsEnabled => true;
        public bool IsMoving => false;
        public double CurrentPosition { get; private set; }
        public bool IsHomed => true;

        public async Task MoveAbsAsync(double position, double speed, CancellationToken ct)
        {
            double distance = Math.Abs(position - CurrentPosition);
            int steps = 20;
            double stepSize = (position - CurrentPosition) / steps;
            for (int i = 0; i < steps && !ct.IsCancellationRequested; i++)
            { CurrentPosition += stepSize; await Task.Delay(10, ct); }
            CurrentPosition = position;
        }

        public async Task MoveRelAsync(double distance, double speed, CancellationToken ct)
            => await MoveAbsAsync(CurrentPosition + distance, speed, ct);

        public Task HomeAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
        public Task EnableAsync(CancellationToken ct) => Task.CompletedTask;
        public Task DisableAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
