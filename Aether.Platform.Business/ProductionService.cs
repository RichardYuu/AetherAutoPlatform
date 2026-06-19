using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class ProductionService : IProductionService
    {
        private readonly IProductionRecordRepository _repository;

        public int TotalCount { get; private set; }
        public int OkCount { get; private set; }
        public int NgCount { get; private set; }
        public int UntestedCount { get; private set; }
        public double YieldRate => TotalCount > 0 ? (double)OkCount / TotalCount * 100 : 0;
        public double OEE { get; private set; }
        public event System.Action OnProductionUpdated;

        public ProductionService(IProductionRecordRepository repository = null)
        {
            _repository = repository;
            if (_repository != null)
            {
                try
                {
                    var stats = _repository.LoadStatsAsync().Result;
                    if (stats.HasValue)
                    {
                        TotalCount = stats.Value.total;
                        OkCount = stats.Value.ok;
                        NgCount = stats.Value.ng;
                        OEE = stats.Value.oee;
                    }
                }
                catch { }
            }
        }

        public void RecordProduction(string serialNumber, string result, Dictionary<string, object> testData)
        {
            TotalCount++;
            if (result == "OK") OkCount++;
            else if (result == "NG") NgCount++;
            else UntestedCount++;
            OnProductionUpdated?.Invoke();
            if (_repository != null)
            {
                try { _repository.SaveStatsAsync(TotalCount, OkCount, NgCount, OEE); }
                catch { }
            }
        }

        public void ResetCount(string userId, string password)
        {
            TotalCount = OkCount = NgCount = UntestedCount = 0;
            if (_repository != null)
            {
                try { _repository.ResetAsync(); }
                catch { }
            }
        }
    }
}
