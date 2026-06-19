namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SimulatedSpotAnalyzer : ISpotAnalyzer
    {
        public string AnalyzerId { get; set; }
        private readonly Random _rng = new Random();

        public Task<SpotResult> AnalyzeAsync(CancellationToken ct)
            => Task.FromResult(new SpotResult
            {
                CentroidX = Math.Round(320 + _rng.NextDouble() * 10 - 5, 2),
                CentroidY = Math.Round(240 + _rng.NextDouble() * 10 - 5, 2),
                Diameter = Math.Round(5.0 + _rng.NextDouble() * 2.0, 2),
                Ellipticity = Math.Round(_rng.NextDouble() * 0.1, 3)
            });
    }
}
