using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces.Services
{
    /// <summary>
    /// 状态服务接口 —— 管理运行时状态（机台状态、权限、件号、点检等）。
    /// 支持断电快照恢复。
    /// </summary>
    public interface IStateService
    {
        /// <summary>当前机台状态</summary>
        MachineStatus MachineStatus { get; }

        /// <summary>当前件号</summary>
        string CurrentPartNumber { get; }

        /// <summary>当前用户 ID</summary>
        string CurrentUser { get; }

        /// <summary>当前用户角色</summary>
        UserRole CurrentRole { get; }

        /// <summary>
        /// 从状态字典中获取指定键的值。
        /// 线程安全（ReaderWriterLockSlim 保护）。
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>值，未找到时返回 default</returns>
        T Get<T>(string key);

        /// <summary>
        /// 设置状态字典中的值。
        /// 线程安全。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        void Set(string key, object value);

        /// <summary>
        /// 设置机台状态。
        /// 触发 OnStatusChanged 事件。
        /// </summary>
        /// <param name="status">新状态</param>
        void SetStatus(MachineStatus status);

        /// <summary>
        /// 设置当前件号。
        /// 触发 OnPartNumberChanged 事件。
        /// </summary>
        /// <param name="partNumber">新件号</param>
        void SetPartNumber(string partNumber);

        /// <summary>
        /// 拍摄状态快照（用于断电恢复）。
        /// 快照压入栈中，可通过 RestoreSnapshot 恢复。
        /// </summary>
        /// <param name="reason">快照原因</param>
        void TakeSnapshot(string reason);

        /// <summary>
        /// 恢复最近一次状态快照。
        /// </summary>
        void RestoreSnapshot();

        /// <summary>机台状态变化事件</summary>
        event Action<MachineStatus> OnStatusChanged;

        /// <summary>件号变化事件</summary>
        event Action<string> OnPartNumberChanged;
    }

    /// <summary>
    /// 参数服务接口 —— 设备/工艺参数的 CRUD 操作。
    /// 支持三种持久化模式：JSON 文件、数据库、JSON+DB 双写。
    /// </summary>
    public interface IParameterService
    {
        /// <summary>参数持久化模式</summary>
        ParameterPersistenceMode PersistenceMode { get; }

        /// <summary>
        /// 加载参数。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <returns>参数对象</returns>
        T Load<T>(string partNumber, string paramName) where T : class, new();

        /// <summary>
        /// 保存参数。
        /// 自动记录变更履历（不可删除）。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <param name="data">参数数据</param>
        void Save<T>(string partNumber, string paramName, T data) where T : class;

        /// <summary>
        /// 导出所有参数到文件。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="filePath">导出文件路径</param>
        void ExportAll(string partNumber, string filePath);

        /// <summary>
        /// 从文件导入所有参数。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="filePath">导入文件路径</param>
        void ImportAll(string partNumber, string filePath);

        /// <summary>
        /// 获取参数变更履历。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <returns>变更履历列表</returns>
        IReadOnlyList<ParameterChangeLog> GetChangeLogs(string partNumber, string paramName);
    }

    /// <summary>
    /// 配置服务接口 —— 应用/硬件配置的加载与热重载。
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 获取配置节。
        /// </summary>
        /// <typeparam name="T">配置节类型</typeparam>
        /// <param name="sectionName">节名称</param>
        /// <returns>配置对象</returns>
        T GetSection<T>(string sectionName) where T : class, new();

        /// <summary>
        /// 获取配置值。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        string GetValue(string key);

        /// <summary>
        /// 设置配置值。
        /// 触发 OnConfigurationChanged 事件。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        void SetValue(string key, string value);

        /// <summary>
        /// 重新加载配置文件。
        /// 触发 OnConfigurationChanged 事件。
        /// </summary>
        void Reload();

        /// <summary>配置变化事件</summary>
        event Action OnConfigurationChanged;
    }

    /// <summary>
    /// 报警服务接口 —— 三级报警管理（Error/Tip/Trace）。
    /// 支持事件通知和 DB 记录。
    /// </summary>
    public interface IAlarmService
    {
        /// <summary>
        /// 触发报警。
        /// </summary>
        /// <param name="level">报警等级</param>
        /// <param name="code">报警码</param>
        /// <param name="message">报警描述</param>
        /// <param name="suggestion">处理建议（可选）</param>
        void Raise(AlarmLevel level, string code, string message, string suggestion = null);

        /// <summary>
        /// 清除报警。
        /// </summary>
        /// <param name="code">报警码</param>
        void Clear(string code);

        /// <summary>获取当前活跃报警列表</summary>
        /// <returns>报警记录列表</returns>
        IReadOnlyList<AlarmRecord> GetActiveAlarms();

        /// <summary>
        /// 获取历史报警记录。
        /// </summary>
        /// <param name="from">起始时间</param>
        /// <param name="to">结束时间</param>
        /// <returns>报警记录列表</returns>
        IReadOnlyList<AlarmRecord> GetHistory(DateTime from, DateTime to);

        /// <summary>报警触发事件</summary>
        event Action<AlarmRecord> OnAlarmRaised;

        /// <summary>报警清除事件</summary>
        event Action<AlarmRecord> OnAlarmCleared;
    }

    /// <summary>
    /// 认证服务接口 —— 多方式登录认证（密码/刷卡/指纹/U 盘狗/IFMS 校验）。
    /// 支持五级权限管理。
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 账号密码登录。
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="password">密码</param>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginAsync(string userId, string password);

        /// <summary>
        /// 刷卡登录。
        /// </summary>
        /// <param name="cardId">卡号</param>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginWithCardAsync(string cardId);

        /// <summary>
        /// 指纹登录（预留）。
        /// </summary>
        /// <param name="data">指纹数据</param>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginWithFingerprintAsync(byte[] data);

        /// <summary>
        /// 人脸识别登录（预留）。
        /// </summary>
        /// <param name="faceData">人脸数据</param>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginWithFaceAsync(byte[] faceData);

        /// <summary>
        /// U 盘密码狗登录。
        /// </summary>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginWithUSBKeyAsync();

        /// <summary>
        /// 验证动态密码（用于产量清零、参数修改等敏感操作）。
        /// </summary>
        /// <param name="password">动态密码</param>
        /// <returns>验证是否通过</returns>
        bool ValidateDynamicPassword(string password);

        /// <summary>
        /// 验证 IFMS 访问权限。
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>验证是否通过</returns>
        bool ValidateIFMSAccess(string userId, string deviceId);

        /// <summary>U 盘密码狗是否已过期</summary>
        bool IsUSBKeyExpired { get; }

        /// <summary>
        /// 激活 U 盘密码狗。
        /// </summary>
        /// <param name="code">激活码</param>
        /// <returns>激活是否成功</returns>
        bool ActivateUSBKey(string code);

        /// <summary>U 盘密码狗有效时长</summary>
        TimeSpan USBKeyValidDuration { get; }

        /// <summary>登出</summary>
        void Logout();
    }

    /// <summary>
    /// 审计服务接口 —— 记录用户操作和参数变更（不可删除）。
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// 记录操作日志。
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="action">操作类型</param>
        /// <param name="detail">详细信息</param>
        void Log(string userId, string action, string detail);

        /// <summary>
        /// 记录参数变更。
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        void LogParameterChange(string userId, string partNumber, string paramName, string oldValue, string newValue);

        /// <summary>
        /// 获取审计日志。
        /// </summary>
        /// <param name="from">起始时间</param>
        /// <param name="to">结束时间</param>
        /// <param name="userId">用户 ID（可选，过滤特定用户）</param>
        /// <returns>日志列表</returns>
        IReadOnlyList<object> GetLogs(DateTime from, DateTime to, string userId = null);
    }

    /// <summary>
    /// 初始化服务接口 —— 硬件/参数分级报错，崩溃异常处理。
    /// </summary>
    public interface IInitializationService
    {
        /// <summary>
        /// 执行初始化。
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>初始化是否成功</returns>
        Task<bool> InitializeAsync(CancellationToken ct);

        /// <summary>是否已完成初始化</summary>
        bool IsInitialized { get; }

        /// <summary>获取初始化错误列表</summary>
        /// <returns>错误信息集合</returns>
        IEnumerable<string> GetErrors();
    }

    /// <summary>
    /// 流程属性服务接口 —— 管理流程动作字典（-3 到 300+）。
    /// </summary>
    public interface IFlowPropertyService
    {
        /// <summary>获取所有流程动作</summary>
        /// <returns>流程动作字典</returns>
        IReadOnlyDictionary<int, FlowAction> GetAllActions();

        /// <summary>
        /// 获取指定编号的流程动作。
        /// </summary>
        /// <param name="code">流程编号</param>
        /// <returns>流程动作</returns>
        FlowAction GetAction(int code);

        /// <summary>
        /// 判断是否为调试动作（100-199）。
        /// </summary>
        /// <param name="code">流程编号</param>
        /// <returns>是否为调试动作</returns>
        bool IsDebugAction(int code);

        /// <summary>
        /// 判断是否为自动运行动作（300+）。
        /// </summary>
        /// <param name="code">流程编号</param>
        /// <returns>是否为自动运行动作</returns>
        bool IsAutoAction(int code);
    }

    /// <summary>
    /// 流程执行服务接口 —— 控制自动流程线程（启动/暂停/停止/复位/急停）。
    /// </summary>
    public interface IFlowRunner
    {
        /// <summary>当前流程状态</summary>
        FlowState CurrentState { get; }

        /// <summary>当前流程步骤编号</summary>
        int CurrentStepCode { get; }

        /// <summary>
        /// 启动自动流程。
        /// </summary>
        /// <param name="startCode">起始流程编号（300+ 为自动运行）</param>
        /// <param name="ct">取消令牌</param>
        Task StartAsync(int startCode, CancellationToken ct);

        /// <summary>
        /// 暂停流程（完成当前动作后挂起）。
        /// </summary>
        Task PauseAsync();

        /// <summary>
        /// 恢复流程。
        /// </summary>
        Task ResumeAsync();

        /// <summary>
        /// 停止流程（安全停止 + 记录位置）。
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 急停（立即停止所有动作，不等待）。
        /// </summary>
        Task EmergencyStopAsync();

        /// <summary>
        /// 复位流程（执行 200-299 复位流程后回到 Idle）。
        /// </summary>
        Task ResetAsync();

        /// <summary>流程状态变化事件</summary>
        event Action<FlowState> OnStateChanged;

        /// <summary>流程步骤变化事件（stepCode, stepName）</summary>
        event Action<int, string> OnStepChanged;
    }

    /// <summary>
    /// 定时任务调度服务接口 —— 管理 7 项定时后台任务。
    /// </summary>
    public interface ITimedTaskScheduler
    {
        /// <summary>
        /// 注册定时任务。
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="interval">执行间隔</param>
        /// <param name="action">任务动作</param>
        void Register(string taskName, TimeSpan interval, Func<CancellationToken, Task> action);

        /// <summary>
        /// 取消定时任务。
        /// </summary>
        /// <param name="taskName">任务名称</param>
        void Unregister(string taskName);

        /// <summary>获取所有任务状态</summary>
        /// <returns>任务状态列表</returns>
        IReadOnlyList<TimedTaskStatus> GetStatus();
    }

    /// <summary>
    /// 多语言服务接口 —— 支持 zh-CN / en / vi-VN 三语切换。
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>当前语言</summary>
        string CurrentLanguage { get; }

        /// <summary>支持的语言列表</summary>
        string[] SupportedLanguages { get; }

        /// <summary>
        /// 切换语言。
        /// 需要重启或刷新 UI。
        /// </summary>
        /// <param name="cultureName">文化名称（"zh-CN" / "en" / "vi-VN"）</param>
        void SwitchTo(string cultureName);

        /// <summary>
        /// 获取翻译文本。
        /// </summary>
        /// <param name="key">翻译键</param>
        /// <returns>翻译文本</returns>
        string T(string key);

        /// <summary>
        /// 获取带参数的翻译文本。
        /// </summary>
        /// <param name="key">翻译键</param>
        /// <param name="args">参数</param>
        /// <returns>翻译文本</returns>
        string T(string key, params object[] args);

        /// <summary>语言变化事件</summary>
        event Action<string> OnLanguageChanged;
    }
}
