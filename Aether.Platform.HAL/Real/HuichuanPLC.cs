namespace Aether.Platform.HAL.Real
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class HuichuanPLC : IPlcCommunication
    {
        public string PlcModel => "Inovance-H5U";
        public bool IsConnected { get; private set; }
        public async Task ConnectAsync(CancellationToken ct) { await Task.Delay(10, ct); IsConnected = true; }
        public Task DisconnectAsync() { IsConnected = false; return Task.CompletedTask; }
        public Task<int> ReadWordAsync(string address, CancellationToken ct) => Task.FromResult(0);
        public Task WriteWordAsync(string address, int value, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ReadBitAsync(string address, CancellationToken ct) => Task.FromResult(false);
        public Task WriteBitAsync(string address, bool value, CancellationToken ct) => Task.CompletedTask;
        public Task<int[]> ReadWordsAsync(string startAddress, int count, CancellationToken ct) => Task.FromResult(new int[count]);
    }
}
