using System.Collections.Generic;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces.Services
{
    public interface IProductionRecordRepository
    {
        Task SaveStatsAsync(int total, int ok, int ng, double oee);
        Task<(int total, int ok, int ng, double oee)?> LoadStatsAsync();
        Task ResetAsync();
    }

    public interface IQualityRecordRepository
    {
        Task SaveMeasurementsAsync(string partNumber, string itemName, List<double> values);
        Task<List<double>> LoadMeasurementsAsync(string partNumber, string itemName);
        Task ClearAsync(string partNumber);
    }

    public interface ITraceabilityRecordRepository
    {
        Task SaveHistoryAsync(string serialNumber, List<StationRecord> records);
        Task<StationHistory> LoadHistoryAsync(string serialNumber);
        Task SaveDuplicateAsync(string stationId, List<string> serialNumbers);
        Task<List<string>> LoadDuplicatesAsync(string stationId);
    }

    public interface IMaintenanceRecordRepository
    {
        Task SavePlanAsync(List<MaintenanceItem> items);
        Task<List<MaintenanceItem>> LoadPlanAsync();
    }

    public interface IParameterPersistenceRepository
    {
        Task<T> LoadAsync<T>(string partNumber, string paramName) where T : class, new();
        Task SaveAsync<T>(string partNumber, string paramName, T data) where T : class;
        Task<IReadOnlyList<ParameterChangeLog>> GetChangeLogsAsync(string partNumber, string paramName);
    }
}