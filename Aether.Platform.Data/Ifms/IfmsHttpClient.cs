using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Data.Configuration;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;
using Newtonsoft.Json;

namespace Aether.Platform.Data.Ifms
{
    public class IfmsHttpClient : IIfmsBroker, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;
        private readonly ConcurrentQueue<IfmsUploadItem> _uploadQueue;
        private readonly object _queueLock = new object();
        private Timer _flushTimer;
        private bool _disposed;

        public bool IsConnected { get; private set; }
        public bool IsEnabled => _config.IfmsEnabled;

        public IfmsHttpClient()
        {
            _config = ConfigManager.Load();
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_config.IfmsBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _uploadQueue = new ConcurrentQueue<IfmsUploadItem>();

            if (_config.IfmsEnabled)
            {
                _flushTimer = new Timer(async _ => await FlushQueueAsync(), null,
                    TimeSpan.FromSeconds(_config.UploadQueueFlushIntervalSec),
                    TimeSpan.FromSeconds(_config.UploadQueueFlushIntervalSec));
            }
        }

        public async Task<StationValidationResult> ValidateStationAsync(StationValidationRequest req)
        {
            if (!IsEnabled) return new StationValidationResult { IsValid = true, Message = "IFMS disabled" };
            try
            {
                var json = JsonConvert.SerializeObject(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync("/station/validate", content);
                if (resp.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    var body = await resp.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<StationValidationResult>(body);
                }
                return new StationValidationResult { IsValid = false, Message = $"HTTP {resp.StatusCode}" };
            }
            catch { IsConnected = false; return new StationValidationResult { IsValid = false, Message = "Network error" }; }
        }

        public async Task<bool> ValidatePartNumberAsync(string partNumber, string deviceId)
        {
            if (!IsEnabled) return true;
            return await PostAsync("/part/validate", new { partNumber, deviceId });
        }

        public async Task<bool> UploadProductionDataAsync(ProductionDataUpload data)
        {
            if (!IsEnabled) return true;
            return await EnqueueOrSend("/production/upload", data);
        }

        public async Task<bool> UploadDeviceStatusAsync(DeviceStatusSnapshot status)
        {
            if (!IsEnabled) return true;
            return await EnqueueOrSend("/device/status", status);
        }

        public async Task<bool> UploadAlarmRecordAsync(AlarmUploadData alarm)
        {
            if (!IsEnabled) return true;
            return await EnqueueOrSend("/alarm/upload", alarm);
        }

        public async Task<bool> UploadParameterSnapshotAsync(string partNumber, object parameters)
        {
            if (!IsEnabled) return true;
            return await EnqueueOrSend("/param/snapshot", new { partNumber, parameters });
        }

        public async Task<bool> UploadQualityDataAsync(QualityReportData report)
        {
            if (!IsEnabled) return true;
            return await EnqueueOrSend("/quality/report", report);
        }

        private async Task<bool> PostAsync(string endpoint, object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync(endpoint, content);
                IsConnected = true;
                return resp.IsSuccessStatusCode;
            }
            catch { IsConnected = false; return false; }
        }

        private async Task<bool> EnqueueOrSend(string endpoint, object data)
        {
            var success = await PostAsync(endpoint, data);
            if (!success)
            {
                _uploadQueue.Enqueue(new IfmsUploadItem
                {
                    Endpoint = endpoint,
                    Data = data,
                    EnqueueTime = DateTime.Now,
                    RetryCount = 0
                });
            }
            return success;
        }

        public async Task FlushQueueAsync()
        {
            var batch = new List<IfmsUploadItem>();
            while (_uploadQueue.TryDequeue(out var item))
            {
                batch.Add(item);
                if (batch.Count >= 50) break;
            }

            foreach (var item in batch)
            {
                var success = await PostAsync(item.Endpoint, item.Data);
                if (!success && item.RetryCount < _config.UploadRetryCount)
                {
                    item.RetryCount++;
                    _uploadQueue.Enqueue(item);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _flushTimer?.Dispose();
                _httpClient?.Dispose();
            }
        }

        private class IfmsUploadItem
        {
            public string Endpoint { get; set; }
            public object Data { get; set; }
            public DateTime EnqueueTime { get; set; }
            public int RetryCount { get; set; }
        }
    }
}
