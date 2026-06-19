using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.DataCollection
{
    public class ProductionDataCollector : IDataCollector
    {
        private readonly IProductionService _productionService;
        private readonly IIfmsBroker _ifmsBroker;
        private Timer _timer;
        private bool _isRunning;

        public event Action<string, object> OnDataCollected;

        public ProductionDataCollector(IProductionService productionService, IIfmsBroker ifmsBroker)
        {
            _productionService = productionService;
            _ifmsBroker = ifmsBroker;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _timer = new Timer(_ => CollectAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }

        private void CollectAsync()
        {
            var data = new
            {
                TotalCount = _productionService.TotalCount,
                OkCount = _productionService.OkCount,
                NgCount = _productionService.NgCount,
                YieldRate = _productionService.YieldRate,
                OEE = _productionService.OEE,
                Timestamp = DateTime.Now
            };
            OnDataCollected?.Invoke("Production", data);
        }
    }
}
