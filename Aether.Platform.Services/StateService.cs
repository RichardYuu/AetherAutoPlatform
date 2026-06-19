using System;
using System.Collections.Generic;
using System.Threading;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Services
{
    public class StateService : IStateService
    {
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Stack<Dictionary<string, object>> _snapshots = new Stack<Dictionary<string, object>>();

        public MachineStatus MachineStatus { get; private set; } = MachineStatus.Idle;
        public string CurrentPartNumber { get; private set; } = string.Empty;
        public string CurrentUser { get; private set; } = string.Empty;
        public UserRole CurrentRole { get; private set; } = UserRole.Operator;

        public event Action<MachineStatus> OnStatusChanged;
        public event Action<string> OnPartNumberChanged;

        public T Get<T>(string key)
        {
            _lock.EnterReadLock();
            try { return _state.TryGetValue(key, out var v) ? (T)v : default; }
            finally { _lock.ExitReadLock(); }
        }

        public void Set(string key, object value)
        {
            _lock.EnterWriteLock();
            try { _state[key] = value; }
            finally { _lock.ExitWriteLock(); }
        }

        public void SetStatus(MachineStatus status)
        {
            MachineStatus = status;
            OnStatusChanged?.Invoke(status);
        }

        public void SetPartNumber(string partNumber)
        {
            CurrentPartNumber = partNumber;
            Set("CurrentPartNumber", partNumber);
            OnPartNumberChanged?.Invoke(partNumber);
        }

        public void TakeSnapshot(string reason)
        {
            _lock.EnterWriteLock();
            try
            {
                var snapshot = new Dictionary<string, object>(_state);
                _snapshots.Push(snapshot);
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void RestoreSnapshot()
        {
            _lock.EnterWriteLock();
            try
            {
                if (_snapshots.Count > 0)
                {
                    var snapshot = _snapshots.Pop();
                    _state.Clear();
                    foreach (var kv in snapshot) _state[kv.Key] = kv.Value;
                }
            }
            finally { _lock.ExitWriteLock(); }
        }
    }
}
