namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedScale : IScaleDevice
    {
        public string ScaleId { get; set; }
        public bool IsConnected => true;
        private double _offset;
        private readonly Random _rng = new Random();

        public Task<double> ReadWeightAsync(CancellationToken ct)
            => Task.FromResult(Math.Round(_offset + _rng.NextDouble() * 0.5, 3));

        public Task ZeroAsync(CancellationToken ct) { _offset = 0; return Task.CompletedTask; }
    }
}
