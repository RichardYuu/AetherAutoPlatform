namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedExposureMeter : IExposureMeter
    {
        public string MeterId { get; set; }
        private readonly Random _rng = new Random();
        private double _accumulated;

        public Task<double> ReadIntensityAsync(CancellationToken ct)
            => Task.FromResult(Math.Round(_rng.NextDouble() * 100.0, 2));

        public async Task<double> AccumulateAsync(TimeSpan duration, CancellationToken ct)
        {
            var steps = (int)(duration.TotalMilliseconds / 100);
            _accumulated = 0;
            for (int i = 0; i < steps && !ct.IsCancellationRequested; i++)
            {
                _accumulated += _rng.NextDouble() * 10;
                await Task.Delay(100, ct);
            }
            return Math.Round(_accumulated, 2);
        }
    }
}
