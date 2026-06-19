using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.DataCollection
{
    public class QualityDataCollector : IDataCollector
    {
        private readonly IIfmsBroker _ifmsBroker;
        private Timer _timer;
        private bool _isRunning;

        public event Action<string, object> OnDataCollected;

        public QualityDataCollector(IIfmsBroker ifmsBroker) { _ifmsBroker = ifmsBroker; }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _timer = new Timer(async _ => await CollectAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }

        private async Task CollectAsync()
        {
            var report = new QualityReportData
            {
                DeviceId = "DEVICE-001",
                PartNumber = "AAAA",
                CPK = 1.33,
                PPK = 1.25,
                ReportTime = DateTime.Now
            };
            OnDataCollected?.Invoke("Quality", report);
            await _ifmsBroker.UploadQualityDataAsync(report);
        }
    }
}
