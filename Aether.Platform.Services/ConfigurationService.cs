using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Data.Configuration;

namespace Aether.Platform.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppConfig _config;
        public event Action OnConfigurationChanged;

        public ConfigurationService()
        {
            _config = ConfigManager.Load();
            ConfigManager.OnConfigChanged += () => OnConfigurationChanged?.Invoke();
        }

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            var config = ConfigManager.Load();
            var json = JsonConvert.SerializeObject(config);
            var all = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (all != null && all.TryGetValue(sectionName, out var section))
            {
                var sectionJson = JsonConvert.SerializeObject(section);
                return JsonConvert.DeserializeObject<T>(sectionJson);
            }
            return new T();
        }

        public string GetValue(string key)
        {
            var config = ConfigManager.Load();
            switch (key)
            {
                case "DeviceId": return config.DeviceId;
                case "DeviceName": return config.DeviceName;
                case "StationId": return config.StationId;
                case "CurrentLanguage": return config.CurrentLanguage;
                case "SqlServerConnectionString": return config.SqlServerConnectionString;
                case "AccessConnectionString": return config.AccessConnectionString;
                default: return ConfigManager.GetValue(key);
            }
        }

        public void SetValue(string key, string value)
        {
            ConfigManager.SetValue(key, value);
            OnConfigurationChanged?.Invoke();
        }

        public void Reload()
        {
            ConfigManager.Load();
            OnConfigurationChanged?.Invoke();
        }
    }
}
