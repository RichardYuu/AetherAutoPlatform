using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Interfaces.HAL
{
    /// <summary>
    /// 轴控制器接口 —— 伺服/步进轴的运动控制。
    /// 支持绝对运动、相对运动、回原点、启停控制。
    /// </summary>
    public interface IAxis
    {
        /// <summary>轴标识符（如 "X"、"Y"、"Z"、"R"）</summary>
        string AxisId { get; }

        /// <summary>轴是否已启用</summary>
        bool IsEnabled { get; }

        /// <summary>轴是否正在运动</summary>
        bool IsMoving { get; }

        /// <summary>当前位置（单位：mm 或 deg）</summary>
        double CurrentPosition { get; }

        /// <summary>是否已完成回原点</summary>
        bool IsHomed { get; }

        /// <summary>
        /// 绝对运动。
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="speed">运动速度</param>
        /// <param name="ct">取消令牌</param>
        Task MoveAbsAsync(double position, double speed, CancellationToken ct);

        /// <summary>
        /// 相对运动。
        /// </summary>
        /// <param name="distance">相对距离</param>
        /// <param name="speed">运动速度</param>
        /// <param name="ct">取消令牌</param>
        Task MoveRelAsync(double distance, double speed, CancellationToken ct);

        /// <summary>
        /// 回原点。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task HomeAsync(CancellationToken ct);

        /// <summary>
        /// 停止当前运动。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task StopAsync(CancellationToken ct);

        /// <summary>
        /// 启用轴（上使能）。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task EnableAsync(CancellationToken ct);

        /// <summary>
        /// 禁用轴（下使能）。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task DisableAsync(CancellationToken ct);
    }

    /// <summary>
    /// 数字 IO 接口 —— 数字输入/输出点的读写。
    /// </summary>
    public interface IDigitalIO
    {
        /// <summary>IO 标识符</summary>
        string IOId { get; }

        /// <summary>是否为输入点（true=输入，false=输出）</summary>
        bool IsInput { get; }

        /// <summary>读取 IO 状态</summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>IO 状态（true/false）</returns>
        Task<bool> ReadAsync(CancellationToken ct);

        /// <summary>
        /// 写入 IO 状态（仅输出点有效）。
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="ct">取消令牌</param>
        Task WriteAsync(bool value, CancellationToken ct);
    }

    /// <summary>
    /// 模拟量 IO 接口 —— 模拟量输入/输出（ADC/DAC）。
    /// 支持电压（0-10V）和电流（4-20mA）模式。
    /// </summary>
    public interface IAnalogIO
    {
        /// <summary>通道号</summary>
        int Channel { get; }

        /// <summary>信号类型（电压或电流）</summary>
        Models.AnalogType Type { get; }

        /// <summary>是否为输出通道</summary>
        bool IsOutput { get; }

        /// <summary>
        /// 读取模拟量值。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>模拟量值（电压：0-10V，电流：4-20mA）</returns>
        Task<double> ReadValueAsync(CancellationToken ct);

        /// <summary>
        /// 写入模拟量值（仅输出通道有效）。
        /// </summary>
        /// <param name="value">要写入的值</param>
        /// <param name="ct">取消令牌</param>
        Task WriteValueAsync(double value, CancellationToken ct);
    }

    /// <summary>
    /// PLC 通信接口 —— 支持基恩士（KV 系列）和汇川（H5U/EASY/AM600）PLC。
    /// </summary>
    public interface IPlcCommunication
    {
        /// <summary>PLC 型号</summary>
        string PlcModel { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>连接 PLC</summary>
        /// <param name="ct">取消令牌</param>
        Task ConnectAsync(CancellationToken ct);

        /// <summary>断开 PLC 连接</summary>
        Task DisconnectAsync();

        /// <summary>
        /// 读取 PLC 字（Word）寄存器。
        /// </summary>
        /// <param name="address">寄存器地址（如 "D100"）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>寄存器值</returns>
        Task<int> ReadWordAsync(string address, CancellationToken ct);

        /// <summary>
        /// 写入 PLC 字（Word）寄存器。
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="ct">取消令牌</param>
        Task WriteWordAsync(string address, int value, CancellationToken ct);

        /// <summary>
        /// 读取 PLC 位（Bit）状态。
        /// </summary>
        /// <param name="address">位地址（如 "M100"）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>位状态</returns>
        Task<bool> ReadBitAsync(string address, CancellationToken ct);

        /// <summary>
        /// 写入 PLC 位（Bit）状态。
        /// </summary>
        /// <param name="address">位地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="ct">取消令牌</param>
        Task WriteBitAsync(string address, bool value, CancellationToken ct);

        /// <summary>
        /// 批量读取 PLC 字寄存器。
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="count">读取数量</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>寄存器值数组</returns>
        Task<int[]> ReadWordsAsync(string startAddress, int count, CancellationToken ct);
    }

    /// <summary>
    /// 扫码器接口 —— 支持基恩士、海康威视、康耐视等扫码器。
    /// </summary>
    public interface IBarcodeScanner
    {
        /// <summary>扫码器标识</summary>
        string ScannerId { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>
        /// 执行扫码。
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>扫码结果（条码字符串）</returns>
        Task<string> ScanAsync(int timeoutMs, CancellationToken ct);

        /// <summary>连接扫码器</summary>
        /// <param name="ct">取消令牌</param>
        Task<bool> ConnectAsync(CancellationToken ct);

        /// <summary>断开扫码器</summary>
        Task DisconnectAsync();
    }

    /// <summary>
    /// 视觉系统接口 —— 支持拍照、定位、匹配、码读取等功能。
    /// 支持 Basler、海康威视、大恒、 Daheng 等相机。
    /// </summary>
    public interface IVisionSystem
    {
        /// <summary>视觉系统标识</summary>
        string VisionId { get; }

        /// <summary>相机型号</summary>
        string CameraModel { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>连接相机</summary>
        /// <param name="ct">取消令牌</param>
        Task<bool> ConnectAsync(CancellationToken ct);

        /// <summary>断开相机</summary>
        Task DisconnectAsync();

        /// <summary>
        /// 拍照。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>图像数据（字节数组）</returns>
        Task<byte[]> CaptureAsync(CancellationToken ct);

        /// <summary>
        /// 视觉定位。
        /// </summary>
        /// <param name="recipe">配方名称</param>
        /// <param name="image">图像数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>定位结果（坐标、角度、分数）</returns>
        Task<Models.VisionResult> LocateAsync(string recipe, byte[] image, CancellationToken ct);

        /// <summary>
        /// 视觉匹配。
        /// </summary>
        /// <param name="recipe">配方名称</param>
        /// <param name="image">图像数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>匹配结果</returns>
        Task<Models.VisionResult> MatchAsync(string recipe, byte[] image, CancellationToken ct);

        /// <summary>
        /// 从图像中读取条码。
        /// </summary>
        /// <param name="image">图像数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>条码字符串</returns>
        Task<string> ReadBarcodeAsync(byte[] image, CancellationToken ct);

        /// <summary>获取相机信息</summary>
        Models.CameraInfo GetCameraInfo();

        /// <summary>
        /// 相机所有者用户 ID。
        /// 用于区分相机占用状态。
        /// </summary>
        string OwnerUserId { get; set; }
    }

    /// <summary>
    /// 串口通信接口 —— 支持 RS232/RS485 串口通信。
    /// </summary>
    public interface ISerialPort
    {
        /// <summary>串口名称</summary>
        string PortName { get; }

        /// <summary>是否已打开</summary>
        bool IsOpen { get; }

        /// <summary>
        /// 打开串口。
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        Task OpenAsync(string portName, int baudRate);

        /// <summary>关闭串口</summary>
        Task CloseAsync();

        /// <summary>
        /// 读取串口数据。
        /// </summary>
        /// <param name="count">读取字节数</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>读取的数据</returns>
        Task<byte[]> ReadAsync(int count, int timeoutMs);

        /// <summary>
        /// 写入串口数据。
        /// </summary>
        /// <param name="data">要写入的数据</param>
        Task WriteAsync(byte[] data);
    }

    /// <summary>
    /// Modbus 通信接口 —— 支持 Modbus RTU 和 Modbus TCP。
    /// </summary>
    public interface IModbusCommunication
    {
        /// <summary>Modbus 类型</summary>
        Models.ModbusType Type { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接 Modbus 设备。
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="ct">取消令牌</param>
        Task ConnectAsync(string connectionString, CancellationToken ct);

        /// <summary>断开 Modbus 连接</summary>
        Task DisconnectAsync();

        /// <summary>
        /// 读取保持寄存器。
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="count">读取数量</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>寄存器值数组</returns>
        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken ct);

        /// <summary>
        /// 写入单个寄存器。
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="ct">取消令牌</param>
        Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken ct);
    }

    /// <summary>
    /// 称重设备接口 —— 电子秤等称重设备。
    /// </summary>
    public interface IScaleDevice
    {
        /// <summary>称重设备标识</summary>
        string ScaleId { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>
        /// 读取重量。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>重量值（单位：g 或 kg）</returns>
        Task<double> ReadWeightAsync(CancellationToken ct);

        /// <summary>
        /// 清零。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task ZeroAsync(CancellationToken ct);
    }

    /// <summary>
    /// 测高仪接口 —— 基恩士 GT 系列、三丰等测高仪。
    /// </summary>
    public interface IHeightGauge
    {
        /// <summary>测高仪标识</summary>
        string GaugeId { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>
        /// 测量高度。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>高度值（单位：mm）</returns>
        Task<double> MeasureAsync(CancellationToken ct);

        /// <summary>
        /// 清零。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task ZeroAsync(CancellationToken ct);

        /// <summary>
        /// 连接测高仪。
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <returns>连接是否成功</returns>
        Task<bool> ConnectAsync(string portName, int baudRate);
    }

    /// <summary>
    /// 温控器接口 —— 欧姆龙 E5CC、汇川等温控器。
    /// </summary>
    public interface ITemperatureController
    {
        /// <summary>温控器标识</summary>
        string ControllerId { get; }

        /// <summary>当前温度</summary>
        double CurrentTemperature { get; }

        /// <summary>设定温度</summary>
        double SetPoint { get; set; }

        /// <summary>是否正在报警</summary>
        bool IsAlarming { get; }

        /// <summary>
        /// 读取温度。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度值（单位：℃）</returns>
        Task<double> ReadTemperatureAsync(CancellationToken ct);

        /// <summary>
        /// 设定温度。
        /// </summary>
        /// <param name="temp">设定温度值</param>
        /// <param name="ct">取消令牌</param>
        Task SetSetPointAsync(double temp, CancellationToken ct);

        /// <summary>启动温控器</summary>
        /// <param name="ct">取消令牌</param>
        Task StartAsync(CancellationToken ct);

        /// <summary>停止温控器</summary>
        /// <param name="ct">取消令牌</param>
        Task StopAsync(CancellationToken ct);
    }

    /// <summary>
    /// 微压计接口 —— 精密气压测量。
    /// </summary>
    public interface IMicropressureSensor
    {
        /// <summary>微压计标识</summary>
        string SensorId { get; }

        /// <summary>当前压力值</summary>
        double Pressure { get; }

        /// <summary>
        /// 读取压力值。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>压力值（单位：Pa）</returns>
        Task<double> ReadAsync(CancellationToken ct);

        /// <summary>
        /// 清零。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task ZeroAsync(CancellationToken ct);
    }

    /// <summary>
    /// 电气比例阀接口 —— SMC ITV 等电气比例阀。
    /// </summary>
    public interface IElectricProportionalValve
    {
        /// <summary>比例阀标识</summary>
        string ValveId { get; }

        /// <summary>输出压力设定值（0-1.0 MPa）</summary>
        double OutputPressure { get; set; }

        /// <summary>
        /// 设定输出压力。
        /// </summary>
        /// <param name="MPa">压力值（MPa）</param>
        /// <param name="ct">取消令牌</param>
        Task SetPressureAsync(double MPa, CancellationToken ct);

        /// <summary>
        /// 读取当前输出压力。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>当前压力值（MPa）</returns>
        Task<double> ReadPressureAsync(CancellationToken ct);
    }

    /// <summary>
    /// 曝光计接口 —— UV 曝光量测量。
    /// </summary>
    public interface IExposureMeter
    {
        /// <summary>曝光计标识</summary>
        string MeterId { get; }

        /// <summary>
        /// 读取当前曝光强度。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>曝光强度（mW/cm²）</returns>
        Task<double> ReadIntensityAsync(CancellationToken ct);

        /// <summary>
        /// 累积曝光量。
        /// </summary>
        /// <param name="duration">曝光时间</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>累积曝光量（mJ/cm²）</returns>
        Task<double> AccumulateAsync(System.TimeSpan duration, CancellationToken ct);
    }

    /// <summary>
    /// 光斑仪接口 —— 光束分析。
    /// </summary>
    public interface ISpotAnalyzer
    {
        /// <summary>光斑仪标识</summary>
        string AnalyzerId { get; }

        /// <summary>
        /// 分析光斑。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>光斑分析结果（质心、直径、椭圆度）</returns>
        Task<Models.SpotResult> AnalyzeAsync(CancellationToken ct);
    }
}
