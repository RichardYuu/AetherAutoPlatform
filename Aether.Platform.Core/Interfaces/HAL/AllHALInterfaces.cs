using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Interfaces.HAL
{
    public interface IAxis
    {
        string AxisId { get; }
        bool IsEnabled { get; }
        bool IsMoving { get; }
        double CurrentPosition { get; }
        bool IsHomed { get; }
        Task MoveAbsAsync(double position, double speed, CancellationToken ct);
        Task MoveRelAsync(double distance, double speed, CancellationToken ct);
        Task HomeAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
        Task EnableAsync(CancellationToken ct);
        Task DisableAsync(CancellationToken ct);
    }

    public interface IDigitalIO
    {
        string IOId { get; }
        bool IsInput { get; }
        Task<bool> ReadAsync(CancellationToken ct);
        Task WriteAsync(bool value, CancellationToken ct);
    }

    public interface IAnalogIO
    {
        int Channel { get; }
        Models.AnalogType Type { get; }
        bool IsOutput { get; }
        Task<double> ReadValueAsync(CancellationToken ct);
        Task WriteValueAsync(double value, CancellationToken ct);
    }

    public interface IPlcCommunication
    {
        string PlcModel { get; }
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken ct);
        Task DisconnectAsync();
        Task<int> ReadWordAsync(string address, CancellationToken ct);
        Task WriteWordAsync(string address, int value, CancellationToken ct);
        Task<bool> ReadBitAsync(string address, CancellationToken ct);
        Task WriteBitAsync(string address, bool value, CancellationToken ct);
        Task<int[]> ReadWordsAsync(string startAddress, int count, CancellationToken ct);
    }

    public interface IBarcodeScanner
    {
        string ScannerId { get; }
        bool IsConnected { get; }
        Task<string> ScanAsync(int timeoutMs, CancellationToken ct);
        Task<bool> ConnectAsync(CancellationToken ct);
        Task DisconnectAsync();
    }

    public interface IVisionSystem
    {
        string VisionId { get; }
        string CameraModel { get; }
        bool IsConnected { get; }
        Task<bool> ConnectAsync(CancellationToken ct);
        Task DisconnectAsync();
        Task<byte[]> CaptureAsync(CancellationToken ct);
        Task<Models.VisionResult> LocateAsync(string recipe, byte[] image, CancellationToken ct);
        Task<Models.VisionResult> MatchAsync(string recipe, byte[] image, CancellationToken ct);
        Task<string> ReadBarcodeAsync(byte[] image, CancellationToken ct);
        Models.CameraInfo GetCameraInfo();
        string OwnerUserId { get; set; }
    }

    public interface ISerialPort
    {
        string PortName { get; }
        bool IsOpen { get; }
        Task OpenAsync(string portName, int baudRate);
        Task CloseAsync();
        Task<byte[]> ReadAsync(int count, int timeoutMs);
        Task WriteAsync(byte[] data);
    }

    public interface IModbusCommunication
    {
        Models.ModbusType Type { get; }
        bool IsConnected { get; }
        Task ConnectAsync(string connectionString, CancellationToken ct);
        Task DisconnectAsync();
        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken ct);
        Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken ct);
    }

    public interface IScaleDevice
    {
        string ScaleId { get; }
        bool IsConnected { get; }
        Task<double> ReadWeightAsync(CancellationToken ct);
        Task ZeroAsync(CancellationToken ct);
    }

    public interface IHeightGauge
    {
        string GaugeId { get; }
        bool IsConnected { get; }
        Task<double> MeasureAsync(CancellationToken ct);
        Task ZeroAsync(CancellationToken ct);
        Task<bool> ConnectAsync(string portName, int baudRate);
    }

    public interface ITemperatureController
    {
        string ControllerId { get; }
        double CurrentTemperature { get; }
        double SetPoint { get; set; }
        bool IsAlarming { get; }
        Task<double> ReadTemperatureAsync(CancellationToken ct);
        Task SetSetPointAsync(double temp, CancellationToken ct);
        Task StartAsync(CancellationToken ct);
        Task StopAsync(CancellationToken ct);
    }

    public interface IMicropressureSensor
    {
        string SensorId { get; }
        double Pressure { get; }
        Task<double> ReadAsync(CancellationToken ct);
        Task ZeroAsync(CancellationToken ct);
    }

    public interface IElectricProportionalValve
    {
        string ValveId { get; }
        double OutputPressure { get; set; }
        Task SetPressureAsync(double MPa, CancellationToken ct);
        Task<double> ReadPressureAsync(CancellationToken ct);
    }

    public interface IExposureMeter
    {
        string MeterId { get; }
        Task<double> ReadIntensityAsync(CancellationToken ct);
        Task<double> AccumulateAsync(System.TimeSpan duration, CancellationToken ct);
    }

    public interface ISpotAnalyzer
    {
        string AnalyzerId { get; }
        Task<Models.SpotResult> AnalyzeAsync(CancellationToken ct);
    }
}