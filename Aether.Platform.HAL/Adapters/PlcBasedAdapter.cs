namespace Aether.Platform.HAL.Adapters
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.HAL;

    public class PlcBasedAdapter
    {
        private readonly IPlcCommunication _plc;
        public PlcBasedAdapter(IPlcCommunication plc) { _plc = plc; }

        public async Task<bool> ReadSensor(string address, CancellationToken ct) => await _plc.ReadBitAsync(address, ct);
        public async Task SetActuator(string address, bool on, CancellationToken ct) => await _plc.WriteBitAsync(address, on, ct);
    }
}
