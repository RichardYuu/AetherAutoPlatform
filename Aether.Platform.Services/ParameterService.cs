using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;
using Aether.Platform.Data.Configuration;
using Aether.Platform.Data.Database;

namespace Aether.Platform.Services
{
    public class ParameterService : IParameterService
    {
        private readonly IDatabaseProvider _dbProvider;
        private readonly string _basePath;

        public ParameterPersistenceMode PersistenceMode { get; private set; }

        public ParameterService(DbContext dbContext = null)
        {
            var config = ConfigManager.Load();

            if (dbContext != null && dbContext.Provider != null)
            {
                _dbProvider = dbContext.Provider;
            }
            else
            {
                try
                {
                    var provider = DatabaseProviderFactory.Create(
                        config.DatabaseMode,
                        config.SqlServerConnectionString,
                        config.AccessConnectionString);
                    _dbProvider = provider;
                }
                catch { _dbProvider = null; }
            }

            _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.ParameterDirectory);
            if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);

            PersistenceMode = config.ParameterPersistenceMode;
        }

        public T Load<T>(string partNumber, string paramName) where T : class, new()
        {
            switch (PersistenceMode)
            {
                case ParameterPersistenceMode.JsonFile:
                    return LoadFromJson<T>(partNumber, paramName);
                case ParameterPersistenceMode.Database:
                    return LoadFromDb<T>(partNumber, paramName);
                case ParameterPersistenceMode.JsonWithDbSync:
                    var json = LoadFromJson<T>(partNumber, paramName);
                    if (json != null) return json;
                    return LoadFromDb<T>(partNumber, paramName);
                default:
                    return new T();
            }
        }

        public void Save<T>(string partNumber, string paramName, T data) where T : class
        {
            switch (PersistenceMode)
            {
                case ParameterPersistenceMode.JsonFile:
                    SaveToJson(partNumber, paramName, data);
                    break;
                case ParameterPersistenceMode.Database:
                    SaveToDb(partNumber, paramName, data);
                    break;
                case ParameterPersistenceMode.JsonWithDbSync:
                    SaveToJson(partNumber, paramName, data);
                    SaveToDb(partNumber, paramName, data);
                    break;
            }
        }

        public void ExportAll(string partNumber, string filePath)
        {
            var partDir = Path.Combine(_basePath, partNumber);
            if (!Directory.Exists(partDir)) return;

            var allParams = new Dictionary<string, object>();
            foreach (var file in Directory.GetFiles(partDir, "*.json"))
            {
                var paramName = Path.GetFileNameWithoutExtension(file);
                var json = File.ReadAllText(file);
                allParams[paramName] = JsonConvert.DeserializeObject(json);
            }

            var exportJson = JsonConvert.SerializeObject(allParams, Formatting.Indented);
            File.WriteAllText(filePath, exportJson);
        }

        public void ImportAll(string partNumber, string filePath)
        {
            if (!File.Exists(filePath)) return;
            var json = File.ReadAllText(filePath);
            var allParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (var kv in allParams)
            {
                SaveToJson(partNumber, kv.Key, kv.Value);
            }
        }

        public IReadOnlyList<ParameterChangeLog> GetChangeLogs(string partNumber, string paramName)
        {
            var logPath = Path.Combine(_basePath, partNumber, paramName + "_changelog.json");
            if (!File.Exists(logPath)) return new List<ParameterChangeLog>();

            var json = File.ReadAllText(logPath);
            return JsonConvert.DeserializeObject<List<ParameterChangeLog>>(json) ?? new List<ParameterChangeLog>();
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

        private static TResult RunSync<TResult>(Func<Task<TResult>> task) => task().GetAwaiter().GetResult();
        private static void RunSync(Func<Task> task) => task().GetAwaiter().GetResult();

        private T LoadFromDb<T>(string partNumber, string paramName) where T : class, new()
        {
            if (_dbProvider == null || !_dbProvider.IsAvailable) return new T();
            try
            {
                var sql = "SELECT ParamValue FROM DeviceParameters WHERE PartNumber=@pn AND ParamName=@name";
                var json = RunSync(() => _dbProvider.QuerySingleAsync<string>(sql, new { pn = partNumber, name = paramName }));
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
                RunSync(() => _dbProvider.ExecuteAsync(sql, new { pn = partNumber, name = paramName, val = json, time = DateTime.Now }));
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
    }
}
