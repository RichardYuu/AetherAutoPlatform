namespace Aether.Platform.HAL.Peripherals
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class ScaleDevice : IScaleDevice
    {
        public string ScaleId { get; }
        public bool IsConnected { get; private set; }

        public ScaleDevice(string scaleId) { ScaleId = scaleId; }

        public Task<double> ReadWeightAsync(CancellationToken ct) => Task.FromResult(0.0);
        public Task ZeroAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
