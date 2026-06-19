namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;

    public class SimulatedSerialPort : ISerialPort
    {
        public string PortName { get; set; }
        public bool IsOpen { get; private set; }
        private readonly Random _rng = new Random();

        public Task OpenAsync(string portName, int baudRate) { PortName = portName; IsOpen = true; return Task.CompletedTask; }
        public Task CloseAsync() { IsOpen = false; return Task.CompletedTask; }
        public async Task<byte[]> ReadAsync(int count, int timeoutMs)
        {
            await Task.Delay(30);
            var buf = new byte[count];
            _rng.NextBytes(buf);
            return buf;
        }
        public Task WriteAsync(byte[] data) => Task.CompletedTask;
    }
}
