namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedScanner : IBarcodeScanner
    {
        public string ScannerId { get; set; }
        public bool IsConnected => true;
        private double _successRate = 0.98;
        private readonly Random _rng = new Random();

        public async Task<string> ScanAsync(int timeoutMs, CancellationToken ct)
        {
            await Task.Delay(50, ct);
            return _rng.NextDouble() < _successRate ? "SIM" + _rng.Next(100000, 999999).ToString() : null;
        }
        public Task<bool> ConnectAsync(CancellationToken ct) => Task.FromResult(true);
        public Task DisconnectAsync() => Task.CompletedTask;
    }
}
