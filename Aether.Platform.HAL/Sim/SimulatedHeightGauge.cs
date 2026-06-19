namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedHeightGauge : IHeightGauge
    {
        public string GaugeId { get; set; }
        public bool IsConnected => true;
        public Task<double> MeasureAsync(CancellationToken ct) => Task.FromResult(10.0 + new Random().NextDouble() * 0.002);
        public Task ZeroAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ConnectAsync(string portName, int baudRate) => Task.FromResult(true);
    }
}
