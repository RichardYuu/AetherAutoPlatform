namespace Aether.Platform.HAL.Peripherals
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class MicropressureSensorDevice : IMicropressureSensor
    {
        public string SensorId { get; }
        public double Pressure { get; private set; }

        public MicropressureSensorDevice(string sensorId) { SensorId = sensorId; }

        public Task<double> ReadAsync(CancellationToken ct) => Task.FromResult(Pressure);
        public Task ZeroAsync(CancellationToken ct) { Pressure = 0; return Task.CompletedTask; }
    }
}
