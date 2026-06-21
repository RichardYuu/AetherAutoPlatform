using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces.Services
{
    /// <summary>
    /// 数据仓储接口 —— 业务数据的持久化存储。
    /// 基于本地 JSON 文件，支持断电恢复。
    /// </summary>
    public interface IDataRepository
    {
        /// <summary>保存数据</summary>
        /// <param name="data">数据对象</param>
        Task SaveAsync(object data);

        /// <summary>加载数据</summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>数据对象</returns>
        Task<T> LoadAsync<T>();

        /// <summary>清空数据</summary>
        Task ClearAsync();
    }

    /// <summary>
    /// 生产记录仓储接口 —— 生产统计数据的持久化。
    /// </summary>
    public interface IProductionRecordRepository
    {
        /// <summary>
        /// 保存生产统计。
        /// </summary>
        /// <param name="total">总数量</param>
        /// <param name="ok">OK 数量</param>
        /// <param name="ng">NG 数量</param>
        /// <param name="oee">OEE</param>
        Task SaveStatsAsync(int total, int ok, int ng, double oee);

        /// <summary>
        /// 加载生产统计。
        /// </summary>
        /// <returns>统计元组（total, ok, ng, oee），未找到时返回 null</returns>
        Task<(int total, int ok, int ng, double oee)?> LoadStatsAsync();

        /// <summary>重置统计数据</summary>
        Task ResetAsync();
    }

    /// <summary>
    /// 品质记录仓储接口 —— 品质测量数据的持久化。
    /// </summary>
    public interface IQualityRecordRepository
    {
        /// <summary>
        /// 保存测量数据。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <param name="values">测量值列表</param>
        Task SaveMeasurementsAsync(string partNumber, string itemName, List<double> values);

        /// <summary>
        /// 加载测量数据。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <returns>测量值列表</returns>
        Task<List<double>> LoadMeasurementsAsync(string partNumber, string itemName);

        /// <summary>
        /// 清空指定件号的所有测量数据。
        /// </summary>
        /// <param name="partNumber">件号</param>
        Task ClearAsync(string partNumber);
    }

    /// <summary>
    /// 追溯记录仓储接口 —— 产品过站历史和重码检测的持久化。
    /// </summary>
    public interface ITraceabilityRecordRepository
    {
        /// <summary>
        /// 保存过站历史。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <param name="records">过站记录列表</param>
        Task SaveHistoryAsync(string serialNumber, List<StationRecord> records);

        /// <summary>
        /// 加载过站历史。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <returns>过站历史</returns>
        Task<StationHistory> LoadHistoryAsync(string serialNumber);

        /// <summary>
        /// 保存重码记录。
        /// </summary>
        /// <param name="stationId">工站 ID</param>
        /// <param name="serialNumbers">重码序列号列表</param>
        Task SaveDuplicateAsync(string stationId, List<string> serialNumbers);

        /// <summary>
        /// 加载重码记录。
        /// </summary>
        /// <param name="stationId">工站 ID</param>
        /// <returns>重码序列号列表</returns>
        Task<List<string>> LoadDuplicatesAsync(string stationId);
    }

    /// <summary>
    /// 维保记录仓储接口 —— 维保计划的持久化。
    /// </summary>
    public interface IMaintenanceRecordRepository
    {
        /// <summary>
        /// 保存维保计划。
        /// </summary>
        /// <param name="items">维保项目列表</param>
        Task SavePlanAsync(List<MaintenanceItem> items);

        /// <summary>
        /// 加载维保计划。
        /// </summary>
        /// <returns>维保项目列表</returns>
        Task<List<MaintenanceItem>> LoadPlanAsync();
    }

    /// <summary>
    /// 参数持久化仓储接口 —— 设备/工艺参数的持久化存储。
    /// 支持 JSON/DB/双写三种模式。
    /// </summary>
    public interface IParameterPersistenceRepository
    {
        /// <summary>
        /// 加载参数。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <returns>参数对象</returns>
        Task<T> LoadAsync<T>(string partNumber, string paramName) where T : class, new();

        /// <summary>
        /// 保存参数。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <param name="data">参数数据</param>
        Task SaveAsync<T>(string partNumber, string paramName, T data);

        /// <summary>
        /// 获取参数变更履历。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="paramName">参数名</param>
        /// <returns>变更履历列表</returns>
        Task<IReadOnlyList<ParameterChangeLog>> GetChangeLogsAsync(string partNumber, string paramName);
    }
}
