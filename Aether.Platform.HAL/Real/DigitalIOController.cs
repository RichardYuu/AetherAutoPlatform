namespace Aether.Platform.HAL.Real
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class DigitalIOController : IDigitalIO
    {
        public string IOId { get; set; }
        public bool IsInput => true;
        public Task<bool> ReadAsync(CancellationToken ct) => Task.FromResult(false);
        public Task WriteAsync(bool value, CancellationToken ct) => Task.CompletedTask;
    }
}
