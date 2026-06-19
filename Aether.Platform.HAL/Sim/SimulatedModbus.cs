namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SimulatedModbus : IModbusCommunication
    {
        public ModbusType Type { get; set; }
        public bool IsConnected { get; private set; }
        private readonly Dictionary<ushort, ushort> _registers = new Dictionary<ushort, ushort>();
        private readonly Random _rng = new Random();

        public Task ConnectAsync(string connectionString, CancellationToken ct) { IsConnected = true; return Task.CompletedTask; }
        public Task DisconnectAsync() { IsConnected = false; return Task.CompletedTask; }
        public Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken ct)
        {
            var result = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                var addr = (ushort)(startAddress + i);
                result[i] = _registers.ContainsKey(addr) ? _registers[addr] : (ushort)_rng.Next(0, 65535);
            }
            return Task.FromResult(result);
        }
        public Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken ct) { _registers[address] = value; return Task.CompletedTask; }
    }
}
