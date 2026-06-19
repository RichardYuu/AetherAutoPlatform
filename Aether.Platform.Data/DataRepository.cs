namespace Aether.Platform.Data
{
    namespace Repository
    {
        using System;
        using System.Collections.Generic;
        using System.IO;
        using System.Linq;
        using System.Threading.Tasks;
        using Configuration;
        using Core.Interfaces;
        using Core.Interfaces.Services;
        using Core.Models;
        using Newtonsoft.Json;

        internal static class RepositoryPath
        {
            public static readonly string BaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            static RepositoryPath()
            {
                if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            }
        }

        public class ProductionRecordRepository : IProductionRecordRepository
        {
            private static readonly string _filePath = Path.Combine(RepositoryPath.BaseDir, "production_stats.json");
            private static readonly object _lock = new object();

            public Task SaveStatsAsync(int total, int ok, int ng, double oee)
            {
                lock (_lock)
                {
                    var data = new { Total = total, Ok = ok, Ng = ng, OEE = oee, LastUpdated = DateTime.Now };
                    File.WriteAllText(_filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                return Task.CompletedTask;
            }

            public Task<(int total, int ok, int ng, double oee)?> LoadStatsAsync()
            {
                lock (_lock)
                {
                    if (!File.Exists(_filePath)) return Task.FromResult(((int, int, int, double)?)null);
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        var data = JsonConvert.DeserializeAnonymousType(json,
                            new { Total = 0, Ok = 0, Ng = 0, OEE = 0.0, LastUpdated = DateTime.MinValue });
                        return Task.FromResult(((int, int, int, double)?)(data.Total, data.Ok, data.Ng, data.OEE));
                    }
                    catch { return Task.FromResult(((int, int, int, double)?)null); }
                }
            }

            public Task ResetAsync()
            {
                lock (_lock)
                {
                    if (File.Exists(_filePath)) File.Delete(_filePath);
                }
                return Task.CompletedTask;
            }
        }

        public class QualityRecordRepository : IQualityRecordRepository
        {
            private static readonly string _baseDir = RepositoryPath.BaseDir;
            private static readonly object _lock = new object();

            private string GetFilePath(string partNumber, string itemName)
            {
                var dir = Path.Combine(_baseDir, "quality");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var safeName = $"{partNumber}_{itemName}".Replace('|', '_').Replace('\\', '_').Replace('/', '_');
                return Path.Combine(dir, $"{safeName}.json");
            }

            public Task SaveMeasurementsAsync(string partNumber, string itemName, List<double> values)
            {
                lock (_lock)
                {
                    var filePath = GetFilePath(partNumber, itemName);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(values));
                }
                return Task.CompletedTask;
            }

            public Task<List<double>> LoadMeasurementsAsync(string partNumber, string itemName)
            {
                lock (_lock)
                {
                    var filePath = GetFilePath(partNumber, itemName);
                    if (!File.Exists(filePath)) return Task.FromResult(new List<double>());
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var values = JsonConvert.DeserializeObject<List<double>>(json);
                        return Task.FromResult(values ?? new List<double>());
                    }
                    catch { return Task.FromResult(new List<double>()); }
                }
            }

            public Task ClearAsync(string partNumber)
            {
                lock (_lock)
                {
                    var dir = Path.Combine(_baseDir, "quality");
                    if (!Directory.Exists(dir)) return Task.CompletedTask;
                    foreach (var file in Directory.GetFiles(dir, $"{partNumber}_*.json"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class TraceabilityRecordRepository : ITraceabilityRecordRepository
        {
            private static readonly string _baseDir = RepositoryPath.BaseDir;
            private static readonly object _lock = new object();

            private string GetHistoryPath(string serialNumber)
            {
                var dir = Path.Combine(_baseDir, "traceability");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, $"{serialNumber}_history.json");
            }

            private string GetDuplicatePath()
            {
                var dir = Path.Combine(_baseDir, "traceability");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "duplicates.json");
            }

            public Task SaveHistoryAsync(string serialNumber, List<StationRecord> records)
            {
                lock (_lock)
                {
                    var filePath = GetHistoryPath(serialNumber);
                    var history = new StationHistory { SerialNumber = serialNumber, Records = records };
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(history, Formatting.Indented));
                }
                return Task.CompletedTask;
            }

            public Task<StationHistory> LoadHistoryAsync(string serialNumber)
            {
                lock (_lock)
                {
                    var filePath = GetHistoryPath(serialNumber);
                    if (!File.Exists(filePath)) return Task.FromResult((StationHistory)null);
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        return Task.FromResult(JsonConvert.DeserializeObject<StationHistory>(json));
                    }
                    catch { return Task.FromResult((StationHistory)null); }
                }
            }

            public Task SaveDuplicateAsync(string stationId, List<string> serialNumbers)
            {
                lock (_lock)
                {
                    var filePath = GetDuplicatePath();
                    var allDups = new Dictionary<string, List<string>>();
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var json = File.ReadAllText(filePath);
                            allDups = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
                        }
                        catch { }
                    }
                    allDups[stationId] = serialNumbers;
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(allDups, Formatting.Indented));
                }
                return Task.CompletedTask;
            }

            public Task<List<string>> LoadDuplicatesAsync(string stationId)
            {
                lock (_lock)
                {
                    var filePath = GetDuplicatePath();
                    if (!File.Exists(filePath)) return Task.FromResult(new List<string>());
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var allDups = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                        if (allDups != null && allDups.TryGetValue(stationId, out var list))
                            return Task.FromResult(list);
                    }
                    catch { }
                    return Task.FromResult(new List<string>());
                }
            }
        }

        public class MaintenanceRecordRepository : IMaintenanceRecordRepository
        {
            private static readonly string _filePath = Path.Combine(RepositoryPath.BaseDir, "maintenance_plan.json");
            private static readonly object _lock = new object();

            public Task SavePlanAsync(List<MaintenanceItem> items)
            {
                lock (_lock)
                {
                    var dir = RepositoryPath.BaseDir;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(_filePath, JsonConvert.SerializeObject(items, Formatting.Indented));
                }
                return Task.CompletedTask;
            }

            public Task<List<MaintenanceItem>> LoadPlanAsync()
            {
                lock (_lock)
                {
                    if (!File.Exists(_filePath)) return Task.FromResult((List<MaintenanceItem>)null);
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        var items = JsonConvert.DeserializeObject<List<MaintenanceItem>>(json);
                        return Task.FromResult(items);
                    }
                    catch { return Task.FromResult((List<MaintenanceItem>)null); }
                }
            }
        }

        public class ParameterPersistenceRepository : IParameterPersistenceRepository
        {
            private readonly IDatabaseProvider _dbProvider;
            private readonly string _basePath;
            private readonly ParameterPersistenceMode _persistenceMode;

            public ParameterPersistenceRepository(IDatabaseProvider dbProvider = null)
            {
                _dbProvider = dbProvider;

                var config = Configuration.ConfigManager.Load();
                _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.ParameterDirectory);
                if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
                _persistenceMode = config.ParameterPersistenceMode;
            }

            public Task<T> LoadAsync<T>(string partNumber, string paramName) where T : class, new()
            {
                switch (_persistenceMode)
                {
                    case ParameterPersistenceMode.Database:
                        return Task.FromResult(LoadFromDb<T>(partNumber, paramName));
                    case ParameterPersistenceMode.JsonWithDbSync:
                        var json = LoadFromJson<T>(partNumber, paramName);
                        if (json != null && !IsDefaultValue(json)) return Task.FromResult(json);
                        return Task.FromResult(LoadFromDb<T>(partNumber, paramName));
                    default:
                        return Task.FromResult(LoadFromJson<T>(partNumber, paramName));
                }
            }

            public Task SaveAsync<T>(string partNumber, string paramName, T data) where T : class
            {
                switch (_persistenceMode)
                {
                    case ParameterPersistenceMode.Database:
                        SaveToDb(partNumber, paramName, data);
                        break;
                    case ParameterPersistenceMode.JsonWithDbSync:
                        SaveToJson(partNumber, paramName, data);
                        SaveToDb(partNumber, paramName, data);
                        break;
                    default:
                        SaveToJson(partNumber, paramName, data);
                        break;
                }
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<ParameterChangeLog>> GetChangeLogsAsync(string partNumber, string paramName)
            {
                var logPath = Path.Combine(_basePath, partNumber, paramName + "_changelog.json");
                if (!File.Exists(logPath)) return Task.FromResult((IReadOnlyList<ParameterChangeLog>)new List<ParameterChangeLog>());
                try
                {
                    var json = File.ReadAllText(logPath);
                    var logs = JsonConvert.DeserializeObject<List<ParameterChangeLog>>(json) ?? new List<ParameterChangeLog>();
                    return Task.FromResult((IReadOnlyList<ParameterChangeLog>)logs.AsReadOnly());
                }
                catch { return Task.FromResult((IReadOnlyList<ParameterChangeLog>)new List<ParameterChangeLog>()); }
            }

            private T LoadFromJson<T>(string partNumber, string paramName) where T : class, new()
            {
                var filePath = Path.Combine(_basePath, partNumber, paramName + ".json");
                if (!File.Exists(filePath)) return new T();
                try
                {
                    var json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch { return new T(); }
            }

            private T LoadFromDb<T>(string partNumber, string paramName) where T : class, new()
            {
                if (_dbProvider == null || !_dbProvider.IsAvailable) return new T();
                try
                {
                    var sql = "SELECT ParamValue FROM DeviceParameters WHERE PartNumber=@pn AND ParamName=@name";
                    var json = _dbProvider.QuerySingleAsync<string>(sql, new { pn = partNumber, name = paramName }).Result;
                    return json != null ? JsonConvert.DeserializeObject<T>(json) : new T();
                }
                catch { return new T(); }
            }

            private void SaveToJson<T>(string partNumber, string paramName, T data)
            {
                var dir = Path.Combine(_basePath, partNumber);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, paramName + ".json");
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);
                AppendChangeLog(partNumber, paramName, null, json);
            }

            private void SaveToDb<T>(string partNumber, string paramName, T data)
            {
                if (_dbProvider == null || !_dbProvider.IsAvailable) return;
                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    var sql = @"IF EXISTS (SELECT 1 FROM DeviceParameters WHERE PartNumber=@pn AND ParamName=@name)
                        UPDATE DeviceParameters SET ParamValue=@val, ModifiedTime=@time WHERE PartNumber=@pn AND ParamName=@name
                        ELSE INSERT INTO DeviceParameters (PartNumber,ParamName,ParamValue,ModifiedTime) VALUES (@pn,@name,@val,@time)";
                    _dbProvider.ExecuteAsync(sql, new { pn = partNumber, name = paramName, val = json, time = DateTime.Now }).Wait();
                }
                catch { }
            }

            private void AppendChangeLog(string partNumber, string paramName, string oldValue, string newValue)
            {
                var dir = Path.Combine(_basePath, partNumber);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var logPath = Path.Combine(dir, paramName + "_changelog.json");
                var logs = new List<ParameterChangeLog>();
                if (File.Exists(logPath))
                {
                    try
                    {
                        var json = File.ReadAllText(logPath);
                        logs = JsonConvert.DeserializeObject<List<ParameterChangeLog>>(json) ?? new List<ParameterChangeLog>();
                    }
                    catch { }
                }
                logs.Add(new ParameterChangeLog
                {
                    PartNumber = partNumber,
                    ParamName = paramName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangedTime = DateTime.Now,
                    ChangedBy = Environment.UserName
                });
                File.WriteAllText(logPath, JsonConvert.SerializeObject(logs, Formatting.Indented));
            }

            private static bool IsDefaultValue<T>(T obj) where T : class, new()
            {
                return obj == null || obj.GetType() == typeof(T) && obj.Equals(new T());
            }
        }
    }
}