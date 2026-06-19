namespace Aether.Platform.HAL.Peripherals
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class ExposureMeterDevice : IExposureMeter
    {
        public string MeterId { get; }

        public ExposureMeterDevice(string meterId) { MeterId = meterId; }

        public Task<double> ReadIntensityAsync(CancellationToken ct) => Task.FromResult(0.0);
        public Task<double> AccumulateAsync(TimeSpan duration, CancellationToken ct) => Task.FromResult(0.0);
    }
}
