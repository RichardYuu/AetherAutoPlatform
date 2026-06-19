using System;
using System.Collections.Generic;
using System.IO;
using Aether.Platform.Core.Models;
using Newtonsoft.Json;

namespace Aether.Platform.Data.Configuration
{
    public static class ConfigManager
    {
        private static readonly object _lock = new object();
        private static AppConfig _config;
        private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "appsettings.json");

        public static AppConfig Load()
        {
            lock (_lock)
            {
                if (_config != null) return _config;

                try
                {
                    var dir = Path.GetDirectoryName(_configPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    if (File.Exists(_configPath))
                    {
                        var json = File.ReadAllText(_configPath);
                        _config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                    }
                    else
                    {
                        _config = new AppConfig();
                        Save(_config);
                    }
                }
                catch
                {
                    _config = new AppConfig();
                }
                return _config;
            }
        }

        public static void Save(AppConfig config = null)
        {
            lock (_lock)
            {
                if (config != null) _config = config;
                if (_config == null) return;

                try
                {
                    var dir = Path.GetDirectoryName(_configPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                    File.WriteAllText(_configPath, json);
                }
                catch { }
            }
        }

        public static string GetValue(string key)
        {
            var config = Load();
            if (config.CustomSettings != null && config.CustomSettings.TryGetValue(key, out var value))
                return value;
            return null;
        }

        public static void SetValue(string key, string value)
        {
            var config = Load();
            if (config.CustomSettings == null)
                config.CustomSettings = new Dictionary<string, string>();
            config.CustomSettings[key] = value;
            Save();
        }

        public static AppConfig GetConfig() => Load();

        public static AppConfig Current => Load();

        public static void SaveToPath(string filePath)
        {
            lock (_lock)
            {
                if (_config == null) return;
                try
                {
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch { }
            }
        }

        public static event Action OnConfigChanged;

        public static void NotifyConfigChanged() => OnConfigChanged?.Invoke();
    }
}
