namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedMicropressure : IMicropressureSensor
    {
        public string SensorId { get; set; }
        public double Pressure { get; private set; }
        public Task<double> ReadAsync(CancellationToken ct)
        {
            Pressure = Math.Round(0.1 + new Random().NextDouble() * 0.9, 3);
            return Task.FromResult(Pressure);
        }
        public Task ZeroAsync(CancellationToken ct) { Pressure = 0; return Task.CompletedTask; }
    }
}
