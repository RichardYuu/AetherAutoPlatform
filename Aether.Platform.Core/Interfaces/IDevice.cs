using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Interfaces
{
    /// <summary>
    /// 设备接口 —— 所有设备适配器必须实现此接口。
    /// 设备是平台的核心抽象，封装了硬件操作、参数管理、状态控制等能力。
    /// </summary>
    /// <remarks>
    /// 两种设备基类：
    /// <list type="bullet">
    /// <item><description><see cref="PlcBasedDevice"/>：PLC 控制模式，运动轴和 IO 由 PLC 控制</description></item>
    /// <item><description><see cref="DirectControlDevice"/>：上位机直控模式，所有硬件由上位机直接控制</description></item>
    /// </list>
    /// </remarks>
    public interface IDevice
    {
        /// <summary>设备类型标识（如 "MTFTest"、"ZG13"）</summary>
        string DeviceType { get; }

        /// <summary>设备名称（中文显示，如 "MTF测试设备"）</summary>
        string DeviceName { get; }

        /// <summary>设备版本号</summary>
        string Version { get; }

        /// <summary>
        /// 初始化设备。
        /// 加载设备配置、初始化硬件连接、生成默认工作流。
        /// </summary>
        /// <returns>初始化是否成功</returns>
        bool Initialize();

        /// <summary>启动设备（开始自动流程）</summary>
        /// <returns>启动是否成功</returns>
        bool Start();

        /// <summary>停止设备（安全停止当前流程）</summary>
        /// <returns>停止是否成功</returns>
        bool Stop();

        /// <summary>关闭设备（释放所有资源、断开硬件连接）</summary>
        /// <returns>关闭是否成功</returns>
        bool Shutdown();

        /// <summary>获取当前设备状态</summary>
        /// <returns><see cref="DeviceStatus"/> 枚举值</returns>
        DeviceStatus GetStatus();

        /// <summary>设备状态变化事件</summary>
        event EventHandler<StatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// 获取设备参数（按件号管理）。
        /// 参数分为三类：标定参数、工艺参数、信息化参数。
        /// </summary>
        /// <returns>包含三类参数的 <see cref="DeviceParameters"/> 对象</returns>
        DeviceParameters GetParameters();

        /// <summary>
        /// 设置设备参数。
        /// 参数变更会自动记录到变更履历（不可删除）。
        /// </summary>
        /// <param name="parameters">新参数对象</param>
        /// <returns>设置是否成功</returns>
        bool SetParameters(DeviceParameters parameters);

        /// <summary>
        /// 执行指定动作。
        /// 用于手动调试或流程引擎调用。
        /// </summary>
        /// <param name="actionName">动作名称</param>
        /// <param name="parameters">动作参数</param>
        /// <returns>执行是否成功</returns>
        bool ExecuteAction(string actionName, Dictionary<string, object> parameters);

        /// <summary>
        /// 获取测试数据（用于 IFMS 上报或调试）。
        /// </summary>
        /// <returns>测试数据字典</returns>
        Dictionary<string, object> GetTestData();

        /// <summary>获取设备支持的所有动作名称列表</summary>
        List<string> GetAvailableActions();

        /// <summary>
        /// 单步执行指定动作（调试模式）。
        /// 仅允许在调试件号下使用（100-199 流程编号）。
        /// </summary>
        /// <param name="action">动作名称</param>
        /// <returns>执行是否成功</returns>
        bool SingleStepExecute(string action);
    }

    /// <summary>
    /// 生产服务接口 —— 管理生产计数、良率、OEE 等统计信息。
    /// </summary>
    public interface IProductionService
    {
        /// <summary>总生产数量</summary>
        int TotalCount { get; }

        /// <summary>OK 数量</summary>
        int OkCount { get; }

        /// <summary>NG 数量</summary>
        int NgCount { get; }

        /// <summary>未测试数量</summary>
        int UntestedCount { get; }

        /// <summary>良率（百分比）</summary>
        double YieldRate { get; }

        /// <summary>设备综合效率 OEE（百分比）</summary>
        double OEE { get; }

        /// <summary>
        /// 记录生产结果。
        /// 自动更新统计并持久化（如果配置了仓储）。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <param name="result">结果（"OK" / "NG" / "Untested"）</param>
        /// <param name="testData">测试数据字典</param>
        void RecordProduction(string serialNumber, string result, Dictionary<string, object> testData);

        /// <summary>
        /// 重置生产计数（需要动态密码权限）。
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="password">动态密码</param>
        void ResetCount(string userId, string password);

        /// <summary>生产数据更新事件</summary>
        event System.Action OnProductionUpdated;
    }

    /// <summary>
    /// 封样服务接口 —— 管理封样记录、自动封样、样品验证。
    /// </summary>
    public interface IGoldenSampleService
    {
        /// <summary>
        /// 创建封样记录。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="sampleName">样品名称</param>
        /// <param name="qrCode">二维码</param>
        /// <param name="testData">测试数据</param>
        void CreateSample(string partNumber, string sampleName, string qrCode, Dictionary<string, object> testData);

        /// <summary>
        /// 自动封样（测试指定次数，可选择使用平均值）。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="testCount">测试次数</param>
        /// <param name="useAverage">是否使用平均值</param>
        void AutoSample(string partNumber, int testCount, bool useAverage);

        /// <summary>
        /// 验证样品是否符合封样标准。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="testData">测试数据</param>
        /// <param name="tolerance">容差</param>
        /// <returns>验证是否通过</returns>
        bool ValidateSample(string partNumber, Dictionary<string, object> testData, double tolerance);

        /// <summary>获取样品历史记录</summary>
        /// <param name="partNumber">件号</param>
        /// <returns>封样记录列表</returns>
        IReadOnlyList<object> GetSampleHistory(string partNumber);
    }
}
