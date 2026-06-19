using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class TraceabilityService : ITraceabilityService
    {
        private readonly ConcurrentDictionary<string, StationHistory> _histories = new ConcurrentDictionary<string, StationHistory>();
        private readonly ConcurrentDictionary<string, HashSet<string>> _duplicates = new ConcurrentDictionary<string, HashSet<string>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ITraceabilityRecordRepository _repository;

        private static readonly string[] RequiredStations = { "ST01", "ST02", "ST03", "ST04", "ST05", "ST06" };

        public TraceabilityService(ITraceabilityRecordRepository repository = null)
        {
            _repository = repository;
            if (_repository != null)
            {
                try { LoadFromRepository(); }
                catch { }
            }
        }

        private void LoadFromRepository()
        {
            foreach (var stationId in RequiredStations)
            {
                var dups = _repository.LoadDuplicatesAsync(stationId).Result;
                if (dups.Count > 0)
                {
                    var set = new HashSet<string>(dups);
                    _duplicates[stationId] = set;
                }
            }
        }

        public TraceResult ValidateProductTrace(string serialNumber, string partNumber, string stationId)
        {
            var result = new TraceResult
            {
                SerialNumber = serialNumber,
                PartNumber = partNumber,
                StationId = stationId,
                TraceTime = DateTime.Now,
                IsDuplicate = IsDuplicate(serialNumber, stationId),
                PassedStations = new List<string>()
            };

            if (result.IsDuplicate)
            {
                result.IsValid = false;
                return result;
            }

            if (_histories.TryGetValue(serialNumber, out var history))
            {
                result.PassedStations = history.Records.Select(r => r.StationId).ToList();
                var missing = RequiredStations.TakeWhile(s => s != stationId)
                    .FirstOrDefault(s => !result.PassedStations.Contains(s));

                if (missing != null)
                {
                    result.IsValid = false;
                    result.MissingStation = missing;
                    return result;
                }
            }

            result.IsValid = true;
            return result;
        }

        public bool RecordStationPass(string serialNumber, string stationId, string operatorId)
        {
            var history = _histories.GetOrAdd(serialNumber, _ => new StationHistory
            {
                SerialNumber = serialNumber,
                Records = new List<StationRecord>()
            });

            var record = new StationRecord
            {
                StationId = stationId,
                OperatorId = operatorId,
                Result = "OK",
                PassTime = DateTime.Now
            };

            _lock.EnterWriteLock();
            try { history.Records.Add(record); }
            finally { _lock.ExitWriteLock(); }

            var dupKey = $"{stationId}";
            var dupSet = _duplicates.GetOrAdd(dupKey, _ => new HashSet<string>());
            lock (dupSet) { dupSet.Add(serialNumber); }

            if (_repository != null)
            {
                try
                {
                    _repository.SaveHistoryAsync(serialNumber, history.Records.ToList());
                    _repository.SaveDuplicateAsync(stationId, dupSet.ToList());
                }
                catch { }
            }

            return true;
        }

        public StationHistory GetStationHistory(string serialNumber)
        {
            _histories.TryGetValue(serialNumber, out var history);
            return history;
        }

        public BatchCompareResult CompareBatchCounts(string batchNumber, int expectedCount)
        {
            var matchingSerials = _histories.Keys
                .Where(k => k.StartsWith(batchNumber))
                .ToList();

            var actualCount = matchingSerials.Count;

            return new BatchCompareResult
            {
                IsMatched = actualCount == expectedCount,
                BatchNumber = batchNumber,
                ExpectedCount = expectedCount,
                ActualCount = actualCount,
                Difference = expectedCount - actualCount,
                Message = actualCount == expectedCount
                    ? "前后道数量一致"
                    : $"数量不匹配: 预期{expectedCount}, 实际{actualCount}, 差值{expectedCount - actualCount}"
            };
        }

        public bool IsDuplicate(string serialNumber, string stationId)
        {
            var dupKey = $"{stationId}";
            if (_duplicates.TryGetValue(dupKey, out var set))
            {
                lock (set) { return set.Contains(serialNumber); }
            }
            return false;
        }
    }
}
