using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Interfaces
{
    public interface IHardwareService
    {
        HAL.IAxis GetAxis(string axisId);
        HAL.IDigitalIO GetDigitalIO(string ioId);
        HAL.IBarcodeScanner GetScanner(string scannerId);
        HAL.IVisionSystem GetVisionSystem(string visionId);
        HAL.IPlcCommunication GetPlc();
        HAL.ISerialPort GetSerialPort(string portName);
        HAL.IModbusCommunication GetModbus(Models.ModbusType type, string connectionString);
        HAL.IAnalogIO GetAnalogIO(int channel);
        HAL.IScaleDevice GetScale(string scaleId);
        HAL.IHeightGauge GetHeightGauge(string gaugeId);
        HAL.ITemperatureController GetTemperature(string tempId);
        HAL.IMicropressureSensor GetMicropressure(string sensorId);
        HAL.IElectricProportionalValve GetEPValve(string valveId);
        HAL.IExposureMeter GetExposureMeter(string meterId);
        HAL.ISpotAnalyzer GetSpotAnalyzer(string analyzerId);
        bool HasPlc { get; }
    }

    public interface IDatabaseProvider : IDisposable
    {
        Models.DatabaseMode Mode { get; }
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);
        Task<int> ExecuteAsync(string sql, object param = null);
        Task<T> QuerySingleAsync<T>(string sql, object param = null);
        bool IsAvailable { get; }
    }

    public interface IIfmsBroker
    {
        bool IsConnected { get; }
        bool IsEnabled { get; }
        Task<Models.StationValidationResult> ValidateStationAsync(Models.StationValidationRequest req);
        Task<bool> ValidatePartNumberAsync(string partNumber, string deviceId);
        Task<bool> UploadProductionDataAsync(Models.ProductionDataUpload data);
        Task<bool> UploadDeviceStatusAsync(Models.DeviceStatusSnapshot status);
        Task<bool> UploadAlarmRecordAsync(Models.AlarmUploadData alarm);
        Task<bool> UploadParameterSnapshotAsync(string partNumber, object parameters);
        Task<bool> UploadQualityDataAsync(Models.QualityReportData report);
        Task FlushQueueAsync();
    }

    public interface IDataCollector
    {
        void Start();
        void Stop();
        event Action<string, object> OnDataCollected;
    }
}