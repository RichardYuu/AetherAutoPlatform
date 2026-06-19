namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SimulatedAnalogIO : IAnalogIO
    {
        public int Channel { get; set; }
        public AnalogType Type { get; set; }
        public bool IsOutput { get; set; }
        private double _value;
        private readonly Random _rng = new Random();

        public Task<double> ReadValueAsync(CancellationToken ct)
        {
            _value = Type == AnalogType.Voltage_0_10V
                ? Math.Round(_rng.NextDouble() * 10.0, 2)
                : Math.Round(4.0 + _rng.NextDouble() * 16.0, 2);
            return Task.FromResult(_value);
        }

        public Task WriteValueAsync(double value, CancellationToken ct) { _value = value; return Task.CompletedTask; }
    }
}
