using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class QualityService : IQualityService
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<double>> _measurements = new ConcurrentDictionary<string, ConcurrentQueue<double>>();
        private readonly HashSet<string> _loadedKeys = new HashSet<string>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly IQualityRecordRepository _repository;
        private const int MaxDataPoints = 500;
        private readonly Random _rng = new Random();

        public QualityService(IQualityRecordRepository repository = null)
        {
            _repository = repository;
        }

        public void RecordMeasurement(string partNumber, string itemName, double value)
        {
            var key = $"{partNumber}|{itemName}";
            if (_repository != null && !_loadedKeys.Contains(key))
            {
                lock (_loadedKeys)
                {
                    if (!_loadedKeys.Contains(key))
                    {
                        _loadedKeys.Add(key);
                        try
                        {
                            var data = _repository.LoadMeasurementsAsync(partNumber, itemName).Result;
                            if (data.Count > 0)
                            {
                                var queue = _measurements.GetOrAdd(key, _ => new ConcurrentQueue<double>());
                                foreach (var val in data) queue.Enqueue(val);
                                while (queue.Count > MaxDataPoints) queue.TryDequeue(out _);
                            }
                        }
                        catch { }
                    }
                }
            }

            var q = _measurements.GetOrAdd(key, _ => new ConcurrentQueue<double>());
            q.Enqueue(value);

            while (q.Count > MaxDataPoints)
                q.TryDequeue(out _);

            if (_repository != null)
            {
                try { _repository.SaveMeasurementsAsync(partNumber, itemName, q.ToList()); }
                catch { }
            }
        }

        public SpcStatistics CalculateStatistics(string partNumber, string itemName)
        {
            var key = $"{partNumber}|{itemName}";
            if (!_measurements.TryGetValue(key, out var queue) || queue.IsEmpty)
                return new SpcStatistics { SampleCount = 0 };

            var data = queue.ToList();
            if (data.Count < 2)
                return new SpcStatistics { SampleCount = data.Count, RawData = data };

            var mean = data.Average();
            var stdDev = Math.Sqrt(data.Sum(d => Math.Pow(d - mean, 2)) / (data.Count - 1));
            var usl = mean + 3 * stdDev;
            var lsl = mean - 3 * stdDev;

            var cpu = (usl - mean) / (3 * stdDev);
            var cpl = (mean - lsl) / (3 * stdDev);
            var cpk = Math.Min(cpu, cpl);

            var pp = (usl - lsl) / (6 * stdDev);
            var ppu = (usl - mean) / (3 * stdDev);
            var ppl = (mean - lsl) / (3 * stdDev);
            var ppk = Math.Min(ppu, ppl);

            return new SpcStatistics
            {
                Mean = Math.Round(mean, 4),
                StdDev = Math.Round(stdDev, 4),
                CPK = Math.Round(cpk, 4),
                PPK = Math.Round(ppk, 4),
                USL = Math.Round(usl, 4),
                LSL = Math.Round(lsl, 4),
                Min = Math.Round(data.Min(), 4),
                Max = Math.Round(data.Max(), 4),
                Range = Math.Round(data.Max() - data.Min(), 4),
                SampleCount = data.Count,
                RawData = data
            };
        }

        public IReadOnlyList<double> GetDataPoints(string partNumber, string itemName, DateTime from, DateTime to)
        {
            var key = $"{partNumber}|{itemName}";
            if (!_measurements.TryGetValue(key, out var queue) || queue.IsEmpty)
                return new List<double>();

            return queue.ToList().AsReadOnly();
        }

        public QualityGrade EvaluateGrade(string partNumber, string itemName)
        {
            var stats = CalculateStatistics(partNumber, itemName);
            if (stats.SampleCount == 0) return QualityGrade.Unknown;

            if (stats.CPK >= 1.67 && stats.PPK >= 1.67) return QualityGrade.Excellent;
            if (stats.CPK >= 1.33 && stats.PPK >= 1.33) return QualityGrade.Good;
            if (stats.CPK >= 1.0 && stats.PPK >= 1.0) return QualityGrade.Acceptable;
            return QualityGrade.Poor;
        }

        public SpcAlarmResult CheckAlarm(string partNumber, string itemName)
        {
            var stats = CalculateStatistics(partNumber, itemName);
            if (stats.SampleCount < 5)
                return new SpcAlarmResult { IsAlarming = false, Level = SpcAlarmLevel.Normal };

            var data = stats.RawData;
            var latest = data[data.Count - 1];

            if (latest > stats.Mean + 3 * stats.StdDev || latest < stats.Mean - 3 * stats.StdDev)
                return new SpcAlarmResult
                {
                    IsAlarming = true,
                    Level = SpcAlarmLevel.StopProduction,
                    RuleViolated = "超出3σ控制限",
                    Message = $"测量值 {latest:F4} 超出 3σ 范围 [{stats.Mean - 3 * stats.StdDev:F4}, {stats.Mean + 3 * stats.StdDev:F4}]"
                };

            if (latest > stats.Mean + 2 * stats.StdDev || latest < stats.Mean - 2 * stats.StdDev)
                return new SpcAlarmResult
                {
                    IsAlarming = true,
                    Level = SpcAlarmLevel.Warning,
                    RuleViolated = "超出2σ预警限",
                    Message = $"测量值 {latest:F4} 超出 2σ 范围"
                };

            int countAbove = 0, countBelow = 0;
            for (int i = data.Count - 7; i < data.Count && i >= 0; i++)
            {
                if (data[i] > stats.Mean) countAbove++;
                else countBelow++;
            }
            if (countAbove >= 7 || countBelow >= 7)
                return new SpcAlarmResult
                {
                    IsAlarming = true,
                    Level = SpcAlarmLevel.Warning,
                    RuleViolated = "连续7点在中心线同侧",
                    Message = $"连续{Math.Max(countAbove, countBelow)}点在中线同侧，存在系统偏移"
                };

            return new SpcAlarmResult { IsAlarming = false, Level = SpcAlarmLevel.Normal };
        }

        public void ClearData(string partNumber)
        {
            var keys = _measurements.Keys.Where(k => k.StartsWith(partNumber + "|")).ToList();
            foreach (var key in keys)
                _measurements.TryRemove(key, out _);
        }
    }
}
