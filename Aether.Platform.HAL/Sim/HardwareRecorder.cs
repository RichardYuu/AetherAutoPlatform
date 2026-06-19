namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Collections.Generic;

    public class HardwareRecorder
    {
        private readonly List<HardwareRecord> _records = new List<HardwareRecord>();
        private bool _isRecording;

        public void StartRecording() { _isRecording = true; }
        public void StopRecording() { _isRecording = false; }
        public IReadOnlyList<HardwareRecord> GetRecords() => _records.AsReadOnly();

        public void Record(string deviceId, string action, object data)
        {
            if (!_isRecording) return;
            _records.Add(new HardwareRecord
            {
                DeviceId = deviceId,
                Action = action,
                Data = data,
                Timestamp = DateTime.Now
            });
        }

        public void ExportToJson(string filePath)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_records, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }

        public class HardwareRecord
        {
            public string DeviceId { get; set; }
            public string Action { get; set; }
            public object Data { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
