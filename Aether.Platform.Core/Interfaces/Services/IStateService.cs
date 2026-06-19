using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces.Services
{
    public interface IStateService
    {
        MachineStatus MachineStatus { get; }
        string CurrentPartNumber { get; }
        string CurrentUser { get; }
        UserRole CurrentRole { get; }

        T Get<T>(string key);
        void Set(string key, object value);
        void SetStatus(MachineStatus status);
        void SetPartNumber(string partNumber);
        void TakeSnapshot(string reason);
        void RestoreSnapshot();
        event Action<MachineStatus> OnStatusChanged;
        event Action<string> OnPartNumberChanged;
    }

    public interface IParameterService
    {
        ParameterPersistenceMode PersistenceMode { get; }
        T Load<T>(string partNumber, string paramName) where T : class, new();
        void Save<T>(string partNumber, string paramName, T data) where T : class;
        void ExportAll(string partNumber, string filePath);
        void ImportAll(string partNumber, string filePath);
        IReadOnlyList<ParameterChangeLog> GetChangeLogs(string partNumber, string paramName);
    }

    public interface IConfigurationService
    {
        T GetSection<T>(string sectionName) where T : class, new();
        string GetValue(string key);
        void SetValue(string key, string value);
        void Reload();
        event Action OnConfigurationChanged;
    }

    public interface IAlarmService
    {
        void Raise(AlarmLevel level, string code, string message, string suggestion = null);
        void Clear(string code);
        IReadOnlyList<AlarmRecord> GetActiveAlarms();
        IReadOnlyList<AlarmRecord> GetHistory(DateTime from, DateTime to);
        event Action<AlarmRecord> OnAlarmRaised;
        event Action<AlarmRecord> OnAlarmCleared;
    }

    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string userId, string password);
        Task<LoginResult> LoginWithCardAsync(string cardId);
        Task<LoginResult> LoginWithFingerprintAsync(byte[] data);
        Task<LoginResult> LoginWithFaceAsync(byte[] faceData);
        Task<LoginResult> LoginWithUSBKeyAsync();
        bool ValidateDynamicPassword(string password);
        bool ValidateIFMSAccess(string userId, string deviceId);
        bool IsUSBKeyExpired { get; }
        bool ActivateUSBKey(string code);
        TimeSpan USBKeyValidDuration { get; }
        void Logout();
    }

    public interface IAuditService
    {
        void Log(string userId, string action, string detail);
        void LogParameterChange(string userId, string partNumber, string paramName, string oldValue, string newValue);
        IReadOnlyList<object> GetLogs(DateTime from, DateTime to, string userId = null);
    }

    public interface IInitializationService
    {
        Task<bool> InitializeAsync(CancellationToken ct);
        bool IsInitialized { get; }
        IEnumerable<string> GetErrors();
    }

    public interface IFlowPropertyService
    {
        IReadOnlyDictionary<int, FlowAction> GetAllActions();
        FlowAction GetAction(int code);
        bool IsDebugAction(int code);
        bool IsAutoAction(int code);
    }

    public interface IFlowRunner
    {
        FlowState CurrentState { get; }
        int CurrentStepCode { get; }
        Task StartAsync(int startCode, CancellationToken ct);
        Task PauseAsync();
        Task ResumeAsync();
        Task StopAsync();
        Task EmergencyStopAsync();
        Task ResetAsync();
        event Action<FlowState> OnStateChanged;
        event Action<int, string> OnStepChanged;
    }

    public interface ITimedTaskScheduler
    {
        void Register(string taskName, TimeSpan interval, Func<CancellationToken, Task> action);
        void Unregister(string taskName);
        IReadOnlyList<TimedTaskStatus> GetStatus();
    }

    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        string[] SupportedLanguages { get; }
        void SwitchTo(string cultureName);
        string T(string key);
        string T(string key, params object[] args);
        event Action<string> OnLanguageChanged;
    }
}