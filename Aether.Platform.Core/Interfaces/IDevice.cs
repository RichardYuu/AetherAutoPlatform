using System;
using System.Collections.Generic;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces
{
    public interface IDevice
    {
        string DeviceType { get; }
        string DeviceName { get; }
        string Version { get; }

        bool Initialize();
        bool Start();
        bool Stop();
        bool Shutdown();

        DeviceStatus GetStatus();
        event EventHandler<StatusChangedEventArgs> StatusChanged;

        DeviceParameters GetParameters();
        bool SetParameters(DeviceParameters parameters);
        bool ExecuteAction(string actionName, Dictionary<string, object> parameters);

        Dictionary<string, object> GetTestData();
        List<string> GetAvailableActions();
        bool SingleStepExecute(string action);
    }

    public interface IProductionService
    {
        int TotalCount { get; }
        int OkCount { get; }
        int NgCount { get; }
        int UntestedCount { get; }
        double YieldRate { get; }
        double OEE { get; }
        void RecordProduction(string serialNumber, string result, Dictionary<string, object> testData);
        void ResetCount(string userId, string password);
        event Action OnProductionUpdated;
    }

    public interface IGoldenSampleService
    {
        void CreateSample(string partNumber, string sampleName, string qrCode, Dictionary<string, object> testData);
        void AutoSample(string partNumber, int testCount, bool useAverage);
        bool ValidateSample(string partNumber, Dictionary<string, object> testData, double tolerance);
        IReadOnlyList<object> GetSampleHistory(string partNumber);
    }
}