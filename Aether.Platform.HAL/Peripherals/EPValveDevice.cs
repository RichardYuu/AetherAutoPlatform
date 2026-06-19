namespace Aether.Platform.HAL.Peripherals
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class EPValveDevice : IElectricProportionalValve
    {
        public string ValveId { get; }
        public double OutputPressure { get; set; }

        public EPValveDevice(string valveId) { ValveId = valveId; }

        public Task SetPressureAsync(double MPa, CancellationToken ct) { OutputPressure = MPa; return Task.CompletedTask; }
        public Task<double> ReadPressureAsync(CancellationToken ct) => Task.FromResult(OutputPressure);
    }
}
