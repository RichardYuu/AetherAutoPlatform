using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Interfaces
{
    /// <summary>
    /// 硬件抽象层统一接口 —— 封装了所有硬件设备的访问方式。
    /// 支持真实硬件和仿真硬件双实现，通过配置切换。
    /// </summary>
    /// <remarks>
    /// 硬件仿真模式（<see cref="Models.SimulationMode"/>）：
    /// <list type="bullet">
    /// <item><description><see cref="Models.SimulationMode.None"/>：完全使用真实硬件</description></item>
    /// <item><description><see cref="Models.SimulationMode.Full"/>：全部硬件仿真（初期开发、UI 调试）</description></item>
    /// <item><description><see cref="Models.SimulationMode.Partial"/>：部分仿真 + 部分真实（渐进式联调）</description></item>
    /// <item><description><see cref="Models.SimulationMode.Replay"/>：回放已录制的真实数据（回归测试）</description></item>
    /// </list>
    /// </remarks>
    public interface IHardwareService
    {
        /// <summary>获取轴控制器</summary>
        /// <param name="axisId">轴标识（如 "X"、"Y"、"Z"、"R"）</param>
        /// <returns><see cref="HAL.IAxis"/> 接口实例</returns>
        HAL.IAxis GetAxis(string axisId);

        /// <summary>获取数字 IO 控制器</summary>
        /// <param name="ioId">IO 标识</param>
        /// <returns><see cref="HAL.IDigitalIO"/> 接口实例</returns>
        HAL.IDigitalIO GetDigitalIO(string ioId);

        /// <summary>获取扫码器</summary>
        /// <param name="scannerId">扫码器标识</param>
        /// <returns><see cref="HAL.IBarcodeScanner"/> 接口实例</returns>
        HAL.IBarcodeScanner GetScanner(string scannerId);

        /// <summary>获取视觉系统</summary>
        /// <param name="visionId">视觉系统标识</param>
        /// <returns><see cref="HAL.IVisionSystem"/> 接口实例</returns>
        HAL.IVisionSystem GetVisionSystem(string visionId);

        /// <summary>获取 PLC 通信接口（仅 PLC 控制模式可用）</summary>
        /// <returns><see cref="HAL.IPlcCommunication"/> 接口实例</returns>
        HAL.IPlcCommunication GetPlc();

        /// <summary>获取串口通信接口</summary>
        /// <param name="portName">串口名称（如 "COM1"）</param>
        /// <returns><see cref="HAL.ISerialPort"/> 接口实例</returns>
        HAL.ISerialPort GetSerialPort(string portName);

        /// <summary>获取 Modbus 通信接口</summary>
        /// <param name="type">Modbus 类型（RTU 或 TCP）</param>
        /// <param name="connectionString">连接字符串</param>
        /// <returns><see cref="HAL.IModbusCommunication"/> 接口实例</returns>
        HAL.IModbusCommunication GetModbus(Models.ModbusType type, string connectionString);

        /// <summary>获取模拟量 IO</summary>
        /// <param name="channel">通道号（0-7）</param>
        /// <returns><see cref="HAL.IAnalogIO"/> 接口实例</returns>
        HAL.IAnalogIO GetAnalogIO(int channel);

        /// <summary>获取称重设备</summary>
        /// <param name="scaleId">称重设备标识</param>
        /// <returns><see cref="HAL.IScaleDevice"/> 接口实例</returns>
        HAL.IScaleDevice GetScale(string scaleId);

        /// <summary>获取测高仪</summary>
        /// <param name="gaugeId">测高仪标识</param>
        /// <returns><see cref="HAL.IHeightGauge"/> 接口实例</returns>
        HAL.IHeightGauge GetHeightGauge(string gaugeId);

        /// <summary>获取温控器</summary>
        /// <param name="tempId">温控器标识</param>
        /// <returns><see cref="HAL.ITemperatureController"/> 接口实例</returns>
        HAL.ITemperatureController GetTemperature(string tempId);

        /// <summary>获取微压计</summary>
        /// <param name="sensorId">微压计标识</param>
        /// <returns><see cref="HAL.IMicropressureSensor"/> 接口实例</returns>
        HAL.IMicropressureSensor GetMicropressure(string sensorId);

        /// <summary>获取电气比例阀</summary>
        /// <param name="valveId">比例阀标识</param>
        /// <returns><see cref="HAL.IElectricProportionalValve"/> 接口实例</returns>
        HAL.IElectricProportionalValve GetEPValve(string valveId);

        /// <summary>获取曝光计</summary>
        /// <param name="meterId">曝光计标识</param>
        /// <returns><see cref="HAL.IExposureMeter"/> 接口实例</returns>
        HAL.IExposureMeter GetExposureMeter(string meterId);

        /// <summary>获取光斑仪</summary>
        /// <param name="analyzerId">光斑仪标识</param>
        /// <returns><see cref="HAL.ISpotAnalyzer"/> 接口实例</returns>
        HAL.ISpotAnalyzer GetSpotAnalyzer(string analyzerId);

        /// <summary>是否使用 PLC 控制模式</summary>
        bool HasPlc { get; }
    }

    /// <summary>
    /// 数据库提供器接口 —— 封装了 SQL Server 和 Access 的数据库操作。
    /// 支持三种模式：单 SQL Server、单 Access、SQL Server 主 + Access 备。
    /// </summary>
    public interface IDatabaseProvider : IDisposable
    {
        /// <summary>数据库模式</summary>
        Models.DatabaseMode Mode { get; }

        /// <summary>
        /// 执行查询，返回多行结果。
        /// 底层使用 Dapper ORM。
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">查询参数</param>
        /// <returns>查询结果集合</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);

        /// <summary>
        /// 执行 SQL 命令（INSERT/UPDATE/DELETE）。
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">命令参数</param>
        /// <returns>受影响的行数</returns>
        Task<int> ExecuteAsync(string sql, object param = null);

        /// <summary>
        /// 执行查询，返回单行结果。
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">查询参数</param>
        /// <returns>单行结果</returns>
        Task<T> QuerySingleAsync<T>(string sql, object param = null);

        /// <summary>数据库是否可用</summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// IFMS 通信接口 —— 负责与 MES 系统的双向交互。
    /// 下行：工站校验、件号校验；上行：7 类数据采集上传。
    /// </summary>
    public interface IIfmsBroker
    {
        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>是否启用 IFMS 功能</summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 验证工站是否允许生产。
        /// 校验项：线体 ID、MAC 地址、软件版本、件号、验收状态、人员资质、点检状态、订单。
        /// </summary>
        /// <param name="req">校验请求</param>
        /// <returns>校验结果</returns>
        Task<Models.StationValidationResult> ValidateStationAsync(Models.StationValidationRequest req);

        /// <summary>验证件号是否允许在当前设备生产</summary>
        /// <param name="partNumber">件号</param>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>验证是否通过</returns>
        Task<bool> ValidatePartNumberAsync(string partNumber, string deviceId);

        /// <summary>上传生产数据</summary>
        /// <param name="data">生产数据</param>
        /// <returns>上传是否成功</returns>
        Task<bool> UploadProductionDataAsync(Models.ProductionDataUpload data);

        /// <summary>上传设备状态快照</summary>
        /// <param name="status">状态快照</param>
        /// <returns>上传是否成功</returns>
        Task<bool> UploadDeviceStatusAsync(Models.DeviceStatusSnapshot status);

        /// <summary>上传报警记录</summary>
        /// <param name="alarm">报警数据</param>
        /// <returns>上传是否成功</returns>
        Task<bool> UploadAlarmRecordAsync(Models.AlarmUploadData alarm);

        /// <summary>上传参数快照</summary>
        /// <param name="partNumber">件号</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>上传是否成功</returns>
        Task<bool> UploadParameterSnapshotAsync(string partNumber, object parameters);

        /// <summary>上传品质数据（SPC 统计）</summary>
        /// <param name="report">品质报告</param>
        /// <returns>上传是否成功</returns>
        Task<bool> UploadQualityDataAsync(Models.QualityReportData report);

        /// <summary>
        /// 刷新离线队列。
        /// 网络恢复后自动补传缓存的数据。
        /// </summary>
        Task FlushQueueAsync();
    }

    /// <summary>
    /// 数据采集接口 —— 定时采集设备状态、生产数据、品质数据并上报 IFMS。
    /// </summary>
    public interface IDataCollector
    {
        /// <summary>启动数据采集</summary>
        void Start();

        /// <summary>停止数据采集</summary>
        void Stop();

        /// <summary>数据采集中事件，用于将采集数据投递到 IFMS 上传队列</summary>
        event Action<string, object> OnDataCollected;
    }
}
