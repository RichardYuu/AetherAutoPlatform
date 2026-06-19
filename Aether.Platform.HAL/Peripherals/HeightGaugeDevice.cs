namespace Aether.Platform.HAL.Peripherals
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class HeightGaugeDevice : IHeightGauge
    {
        private bool _isConnected;

        public string GaugeId { get; }
        public bool IsConnected => _isConnected;

        public HeightGaugeDevice(string gaugeId) { GaugeId = gaugeId; }

        public Task<double> MeasureAsync(CancellationToken ct) => Task.FromResult(0.0);
        public Task ZeroAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ConnectAsync(string portName, int baudRate)
        {
            try { _isConnected = true; return Task.FromResult(true); }
            catch { return Task.FromResult(false); }
        }
    }
}
