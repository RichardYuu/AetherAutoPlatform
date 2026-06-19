namespace Aether.Platform.HAL.Peripherals
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SpotAnalyzerDevice : ISpotAnalyzer
    {
        public string AnalyzerId { get; }

        public SpotAnalyzerDevice(string analyzerId) { AnalyzerId = analyzerId; }

        public Task<SpotResult> AnalyzeAsync(CancellationToken ct)
            => Task.FromResult(new SpotResult());
    }
}
