using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Services
{
    public class AlarmService : IAlarmService
    {
        private readonly ConcurrentDictionary<string, AlarmRecord> _activeAlarms = new ConcurrentDictionary<string, AlarmRecord>();
        private readonly List<AlarmRecord> _history = new List<AlarmRecord>();
        private readonly ReaderWriterLockSlim _historyLock = new ReaderWriterLockSlim();

        public event Action<AlarmRecord> OnAlarmRaised;
        public event Action<AlarmRecord> OnAlarmCleared;

        public void Raise(AlarmLevel level, string code, string message, string suggestion = null)
        {
            var record = new AlarmRecord
            {
                Code = code,
                Level = level,
                Description = message,
                Suggestion = suggestion,
                OccurTime = DateTime.Now,
                IsActive = true
            };

            _activeAlarms[code] = record;

            _historyLock.EnterWriteLock();
            try { _history.Add(record); }
            finally { _historyLock.ExitWriteLock(); }

            OnAlarmRaised?.Invoke(record);
        }

        public void Clear(string code)
        {
            if (_activeAlarms.TryRemove(code, out var record))
            {
                record.IsActive = false;
                record.ClearedTime = DateTime.Now;

                _historyLock.EnterWriteLock();
                try
                {
                    var existing = _history.Find(a => a.Code == code && a.IsActive);
                    if (existing != null)
                    {
                        existing.IsActive = false;
                        existing.ClearedTime = DateTime.Now;
                    }
                    else
                    {
                        _history.Add(record);
                    }
                }
                finally { _historyLock.ExitWriteLock(); }

                OnAlarmCleared?.Invoke(record);
            }
        }

        public void ClearAll()
        {
            foreach (var code in _activeAlarms.Keys)
                Clear(code);
        }

        public IReadOnlyList<AlarmRecord> GetActiveAlarms()
        {
            return new List<AlarmRecord>(_activeAlarms.Values).AsReadOnly();
        }

        public IReadOnlyList<AlarmRecord> GetHistory(DateTime from, DateTime to)
        {
            _historyLock.EnterReadLock();
            try
            {
                return _history.FindAll(a => a.OccurTime >= from && a.OccurTime <= to).AsReadOnly();
            }
            finally { _historyLock.ExitReadLock(); }
        }
    }
}
