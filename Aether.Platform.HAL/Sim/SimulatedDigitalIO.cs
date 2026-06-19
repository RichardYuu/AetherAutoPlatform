namespace Aether.Platform.HAL.Sim
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedDigitalIO : IDigitalIO
    {
        public string IOId { get; set; }
        public bool IsInput => true;
        private bool _value;
        public Task<bool> ReadAsync(CancellationToken ct) => Task.FromResult(_value);
        public Task WriteAsync(bool value, CancellationToken ct) { _value = value; return Task.CompletedTask; }
    }
}
