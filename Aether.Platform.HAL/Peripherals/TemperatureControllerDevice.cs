namespace Aether.Platform.HAL.Peripherals
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class TemperatureControllerDevice : ITemperatureController
    {
        public string ControllerId { get; }
        public double CurrentTemperature { get; private set; }
        public double SetPoint { get; set; }
        public bool IsAlarming { get; private set; }

        public TemperatureControllerDevice(string controllerId) { ControllerId = controllerId; }

        public Task<double> ReadTemperatureAsync(CancellationToken ct) => Task.FromResult(CurrentTemperature);
        public Task SetSetPointAsync(double temp, CancellationToken ct) { SetPoint = temp; return Task.CompletedTask; }
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
