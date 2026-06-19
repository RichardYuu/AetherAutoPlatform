using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Models
{
    public class FlowAction
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FlowCategory Category { get; set; }
        public Func<CancellationToken, Task<bool>> Execute { get; set; }
    }

    public class ParameterChangeLog
    {
        public long Id { get; set; }
        public string PartNumber { get; set; }
        public string ParamName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedTime { get; set; }
    }

    public class TimedTaskStatus
    {
        public string TaskName { get; set; }
        public DateTime LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public bool IsRunning { get; set; }
        public string LastError { get; set; }
    }

    public class StatusMessage
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public AlarmLevel Level { get; set; }
        public DateTime Time { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public UserRole Role { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class StationValidationRequest
    {
        public string DeviceId { get; set; }
        public string LineId { get; set; }
        public string MacAddress { get; set; }
        public string SoftwareVersion { get; set; }
        public string PartNumber { get; set; }
        public string UserId { get; set; }
        public string OrderNumber { get; set; }
    }

    public class StationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> FailedChecks { get; set; }
        public string Message { get; set; }
    }

    public class DeviceStatusSnapshot
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public MachineStatus Status { get; set; }
        public string PartNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public double OEE { get; set; }
    }

    public class ProductionDataUpload
    {
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string StationId { get; set; }
        public string Result { get; set; }
        public Dictionary<string, object> TestData { get; set; }
        public DateTime TestTime { get; set; }
    }

    public class QualityReportData
    {
        public string DeviceId { get; set; }
        public string PartNumber { get; set; }
        public double CPK { get; set; }
        public double PPK { get; set; }
        public DateTime ReportTime { get; set; }
    }

    public class AlarmUploadData
    {
        public string AlarmCode { get; set; }
        public AlarmLevel Level { get; set; }
        public string Description { get; set; }
        public DateTime OccurTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class TrayStateMachine
    {
        public string StorageId { get; set; }
        public int LayerIndex { get; set; }
        public TrayState State { get; set; }
        public int TotalSlots { get; set; }
        public int FinishedCount { get; set; }
        public int OkCount { get; set; }
        public int NgCount { get; set; }
        public int UntestedCount { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ConfirmedTime { get; set; }
        public DateTime? StartedTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public int VisualVerifyCount { get; set; }
        public bool VisualVerified { get; set; }
    }

    public class LocalizedString
    {
        public string zhCN { get; set; }
        public string en { get; set; }
        public string viVN { get; set; }

        public string Get(string culture)
        {
            switch (culture)
            {
                case "en": return en ?? zhCN;
                case "vi-VN": return viVN ?? zhCN;
                default: return zhCN;
            }
        }
    }

    public class DeviceParameters
    {
        public string PartNumber { get; set; }
        public Dictionary<string, object> CalibrationParams { get; set; }
        public Dictionary<string, object> ProcessParams { get; set; }
        public Dictionary<string, object> InfoParams { get; set; }
    }
}