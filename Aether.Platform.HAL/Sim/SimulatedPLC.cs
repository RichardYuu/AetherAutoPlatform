namespace Aether.Platform.HAL.Sim
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedPLC : IPlcCommunication
    {
        private readonly Dictionary<string, int> _words = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _bits = new Dictionary<string, bool>();
        public string PlcModel => "Simulated-PLC";
        public bool IsConnected => true;
        public Task ConnectAsync(CancellationToken ct) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<int> ReadWordAsync(string address, CancellationToken ct) => Task.FromResult(_words.ContainsKey(address) ? _words[address] : 0);
        public Task WriteWordAsync(string address, int value, CancellationToken ct) { _words[address] = value; return Task.CompletedTask; }
        public Task<bool> ReadBitAsync(string address, CancellationToken ct) => Task.FromResult(_bits.ContainsKey(address) && _bits[address]);
        public Task WriteBitAsync(string address, bool value, CancellationToken ct) { _bits[address] = value; return Task.CompletedTask; }
        public Task<int[]> ReadWordsAsync(string startAddress, int count, CancellationToken ct) => Task.FromResult(new int[count]);
    }
}
