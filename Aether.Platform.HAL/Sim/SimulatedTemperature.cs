namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedTemperature : ITemperatureController
    {
        public string ControllerId { get; set; }
        public double CurrentTemperature { get; private set; } = 25.0;
        public double SetPoint { get; set; } = 25.0;
        public bool IsAlarming => false;
        public Task<double> ReadTemperatureAsync(CancellationToken ct)
        {
            var drift = (new Random().NextDouble() - 0.5) * 0.2;
            CurrentTemperature = SetPoint + drift;
            return Task.FromResult(CurrentTemperature);
        }
        public Task SetSetPointAsync(double temp, CancellationToken ct) { SetPoint = temp; return Task.CompletedTask; }
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
