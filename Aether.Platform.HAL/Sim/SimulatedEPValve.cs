namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedEPValve : IElectricProportionalValve
    {
        public string ValveId { get; set; }
        public double OutputPressure { get; set; } = 0.5;
        public Task SetPressureAsync(double MPa, CancellationToken ct) { OutputPressure = MPa; return Task.CompletedTask; }
        public Task<double> ReadPressureAsync(CancellationToken ct)
        {
            var noise = (new Random().NextDouble() - 0.5) * 0.02;
            return Task.FromResult(Math.Round(OutputPressure + noise, 3));
        }
    }
}
