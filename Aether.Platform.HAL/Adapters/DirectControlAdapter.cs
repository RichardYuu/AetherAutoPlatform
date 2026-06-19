namespace Aether.Platform.HAL.Adapters
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces;

    public class DirectControlAdapter
    {
        private readonly IHardwareService _hardware;
        public DirectControlAdapter(IHardwareService hardware) { _hardware = hardware; }

        public Task<double> GetAnalogValue(int channel, CancellationToken ct)
            => _hardware.GetAnalogIO(channel)?.ReadValueAsync(ct) ?? Task.FromResult(0.0);

        public Task SetAnalogValue(int channel, double value, CancellationToken ct)
            => _hardware.GetAnalogIO(channel)?.WriteValueAsync(value, ct) ?? Task.CompletedTask;
    }
}
