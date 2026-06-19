using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces
{
    public interface ICheckListService
    {
        IReadOnlyList<CheckListItem> GetCheckList(string partNumber);
        CheckResult Check(string partNumber, string userId);
        bool IsChecked(string partNumber);
        void ForceRecheck(string partNumber);
        void SetSchedule(string partNumber, TimeSpan interval);
        event Action<string, CheckResult> OnCheckCompleted;
    }

    public interface IQualityService
    {
        void RecordMeasurement(string partNumber, string itemName, double value);
        SpcStatistics CalculateStatistics(string partNumber, string itemName);
        IReadOnlyList<double> GetDataPoints(string partNumber, string itemName, DateTime from, DateTime to);
        QualityGrade EvaluateGrade(string partNumber, string itemName);
        SpcAlarmResult CheckAlarm(string partNumber, string itemName);
        void ClearData(string partNumber);
    }

    public interface ITraceabilityService
    {
        TraceResult ValidateProductTrace(string serialNumber, string partNumber, string stationId);
        bool RecordStationPass(string serialNumber, string stationId, string operatorId);
        StationHistory GetStationHistory(string serialNumber);
        BatchCompareResult CompareBatchCounts(string batchNumber, int expectedCount);
        bool IsDuplicate(string serialNumber, string stationId);
    }

    public interface IMaintenanceService
    {
        IReadOnlyList<MaintenanceItem> GetPlan(string deviceId);
        MaintenanceStatus CheckItem(string itemId);
        void RecordExecution(string itemId, string userId, string notes);
        bool IsOverdue(string itemId);
        void AddCustomItem(string itemId, string name, MaintenanceFrequency frequency, int countThreshold);
        void UpdateAxisCount(string axisId, int operationCount);
        void UpdatePneumaticCount(string componentId, int operationCount);
        IReadOnlyList<MaintenanceAlert> GetAlerts(string deviceId);
    }

    public interface IExportService
    {
        Task<ExportResult> ExportProductionDaily(DateTime date, string outputPath);
        Task<ExportResult> ExportAlarmsDaily(DateTime date, string outputPath);
        Task<ExportResult> ExportParameters(string partNumber, string outputPath);
        bool IsNetworkAvailable { get; }
        IReadOnlyList<PendingExport> GetPendingExports();
        Task FlushPendingExports();
    }
}

namespace Aether.Platform.Core.Models
{
    public class CheckListItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public CheckItemCategory Category { get; set; }
        public bool IsRequired { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string Unit { get; set; }
        public bool IsChecked { get; set; }
        public double? MeasuredValue { get; set; }
        public bool Result { get; set; }
    }

    public class CheckResult
    {
        public bool AllPassed { get; set; }
        public string PartNumber { get; set; }
        public string UserId { get; set; }
        public DateTime CheckTime { get; set; }
        public List<CheckListItem> Items { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class CheckSchedule
    {
        public string PartNumber { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime LastCheckTime { get; set; }
        public DateTime NextCheckTime { get; set; }
        public bool IsOverdue => DateTime.Now > NextCheckTime;
    }

    public class SpcStatistics
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double CPK { get; set; }
        public double PPK { get; set; }
        public double USL { get; set; }
        public double LSL { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Range { get; set; }
        public int SampleCount { get; set; }
        public List<double> RawData { get; set; }
    }

    public class SpcAlarmResult
    {
        public bool IsAlarming { get; set; }
        public SpcAlarmLevel Level { get; set; }
        public string RuleViolated { get; set; }
        public string Message { get; set; }
    }

    public class TraceResult
    {
        public bool IsValid { get; set; }
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string StationId { get; set; }
        public List<string> PassedStations { get; set; }
        public string MissingStation { get; set; }
        public bool IsDuplicate { get; set; }
        public DateTime TraceTime { get; set; }
    }

    public class StationHistory
    {
        public string SerialNumber { get; set; }
        public List<StationRecord> Records { get; set; }
    }

    public class StationRecord
    {
        public string StationId { get; set; }
        public string OperatorId { get; set; }
        public string Result { get; set; }
        public DateTime PassTime { get; set; }
        public Dictionary<string, object> TestData { get; set; }
    }

    public class BatchCompareResult
    {
        public bool IsMatched { get; set; }
        public string BatchNumber { get; set; }
        public int ExpectedCount { get; set; }
        public int ActualCount { get; set; }
        public int Difference { get; set; }
        public string Message { get; set; }
    }

    public class MaintenanceItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MaintenanceFrequency Frequency { get; set; }
        public int CountThreshold { get; set; }
        public int CurrentCount { get; set; }
        public DateTime LastExecution { get; set; }
        public DateTime NextDueDate { get; set; }
        public string LastOperator { get; set; }
        public bool IsOverdue => DateTime.Now > NextDueDate;
    }

    public class MaintenanceAlert
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public MaintenanceAlertLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime AlertTime { get; set; }
    }

    public class MaintenanceStatus
    {
        public string ItemId { get; set; }
        public bool IsOverdue { get; set; }
        public int RemainingCount { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public bool NeedsAttention { get; set; }
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
        public int RecordCount { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public class PendingExport
    {
        public string Id { get; set; }
        public ExportType Type { get; set; }
        public DateTime Date { get; set; }
        public string OutputPath { get; set; }
        public int RetryCount { get; set; }
        public string LastError { get; set; }
    }

    public enum CheckItemCategory
    {
        Safety,
        Hardware,
        Calibration,
        Environment,
        Consumable
    }

    public enum SpcAlarmLevel
    {
        Normal,
        Warning,
        StopProduction
    }

    public enum QualityGrade
    {
        Unknown,
        Poor,
        Acceptable,
        Good,
        Excellent
    }

    public enum MaintenanceFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        ByOperationCount,
        ByMileage,
        Custom
    }

    public enum MaintenanceAlertLevel
    {
        Info,
        Warning,
        Critical
    }

    public enum ExportType
    {
        Production,
        Alarms,
        Parameters,
        Quality
    }

    public enum BatchStatus
    {
        InProgress,
        Matched,
        Mismatched,
        Incomplete
    }
}