using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Data.Configuration;

namespace Aether.Platform.Services
{
    public class AuditService : IAuditService
    {
        private List<AuditEntry> _auditLogs = new List<AuditEntry>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly string _logPath;

        public AuditService()
        {
            var config = ConfigManager.Load();
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.LogDirectory, "audit.json");
            var dir = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            LoadFromDisk();
        }

        public void Log(string userId, string action, string detail)
        {
            var entry = new AuditEntry
            {
                UserId = userId,
                Action = action,
                Detail = detail,
                Timestamp = DateTime.Now
            };
            _lock.EnterWriteLock();
            try { _auditLogs.Add(entry); }
            finally { _lock.ExitWriteLock(); }
            SaveToDisk();
        }

        public void LogParameterChange(string userId, string partNumber, string paramName, string oldValue, string newValue)
        {
            Log(userId, "ParameterChange", $"{partNumber}/{paramName}: {oldValue} -> {newValue}");
        }

        public IReadOnlyList<object> GetLogs(DateTime from, DateTime to, string userId = null)
        {
            _lock.EnterReadLock();
            try
            {
                var filtered = _auditLogs.FindAll(e => e.Timestamp >= from && e.Timestamp <= to);
                if (userId != null)
                    filtered = filtered.FindAll(e => e.UserId == userId);
                return filtered.ConvertAll(e => (object)e).AsReadOnly();
            }
            finally { _lock.ExitReadLock(); }
        }

        private void SaveToDisk()
        {
            try
            {
                _lock.EnterReadLock();
                try
                {
                    var json = JsonConvert.SerializeObject(_auditLogs, Formatting.Indented);
                    File.WriteAllText(_logPath, json);
                }
                finally { _lock.ExitReadLock(); }
            }
            catch { }
        }

        private void LoadFromDisk()
        {
            try
            {
                if (File.Exists(_logPath))
                {
                    var json = File.ReadAllText(_logPath);
                    var logs = JsonConvert.DeserializeObject<List<AuditEntry>>(json);
                    if (logs != null) _auditLogs = logs;
                }
            }
            catch { }
        }

        private class AuditEntry
        {
            public string UserId { get; set; }
            public string Action { get; set; }
            public string Detail { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
