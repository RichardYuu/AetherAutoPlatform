using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.DataCollection
{
    public class DeviceStatusCollector : IDataCollector
    {
        private readonly IStateService _stateService;
        private readonly IIfmsBroker _ifmsBroker;
        private Timer _timer;
        private bool _isRunning;

        public event Action<string, object> OnDataCollected;

        public DeviceStatusCollector(IStateService stateService, IIfmsBroker ifmsBroker)
        {
            _stateService = stateService;
            _ifmsBroker = ifmsBroker;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _timer = new Timer(async _ => await CollectAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }

        private async Task CollectAsync()
        {
            var snapshot = new DeviceStatusSnapshot
            {
                DeviceId = "DEVICE-001",
                DeviceType = "自动化设备",
                Status = _stateService.MachineStatus,
                PartNumber = _stateService.CurrentPartNumber,
                Timestamp = DateTime.Now,
                OEE = 0.0
            };
            OnDataCollected?.Invoke("DeviceStatus", snapshot);
            await _ifmsBroker.UploadDeviceStatusAsync(snapshot);
        }
    }
}
