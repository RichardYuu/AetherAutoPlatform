using System.Collections.Generic;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Data.Configuration
{
    public class AppConfig
    {
        public DatabaseMode DatabaseMode { get; set; }
        public string SqlServerConnectionString { get; set; }
        public string AccessConnectionString { get; set; }
        public ParameterPersistenceMode ParameterPersistenceMode { get; set; }
        public SimulationMode SimulationMode { get; set; }
        public bool IfmsEnabled { get; set; }
        public string IfmsBaseUrl { get; set; }
        public string StationId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string MacAddress { get; set; }
        public string ParameterDirectory { get; set; }
        public string LogDirectory { get; set; }
        public string CurrentLanguage { get; set; }
        public int UploadRetryCount { get; set; }
        public int UploadRetryIntervalMs { get; set; }
        public int UploadQueueFlushIntervalSec { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; }

        public AppConfig()
        {
            DatabaseMode = DatabaseMode.AccessOnly;
            SqlServerConnectionString = "Server=.;Database=AutoPlatform;Integrated Security=True;";
            AccessConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Data/AutoPlatform.accdb;";
            ParameterPersistenceMode = ParameterPersistenceMode.JsonFile;
            SimulationMode = SimulationMode.Full;
            IfmsEnabled = false;
            IfmsBaseUrl = "http://localhost:8080/api";
            StationId = "STATION-001";
            DeviceId = "DEVICE-001";
            DeviceName = "自动化设备";
            ParameterDirectory = "Parameters";
            LogDirectory = "Logs";
            CurrentLanguage = "zh-CN";
            UploadRetryCount = 3;
            UploadRetryIntervalMs = 2000;
            UploadQueueFlushIntervalSec = 10;
            CustomSettings = new Dictionary<string, string>();
        }
    }
}
