using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces.Services
{
    /// <summary>
    /// 点检服务接口 —— 管理开机/换件号强制点检。
    /// 支持 5 类点检：安全/硬件/标定/环境/耗材。
    /// </summary>
    public interface ICheckListService
    {
        /// <summary>
        /// 获取点检清单。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <returns>点检项目列表</returns>
        IReadOnlyList<CheckListItem> GetCheckList(string partNumber);

        /// <summary>
        /// 执行点检。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="userId">操作员 ID</param>
        /// <returns>点检结果</returns>
        CheckResult Check(string partNumber, string userId);

        /// <summary>
        /// 判断是否已完成点检。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <returns>是否已点检</returns>
        bool IsChecked(string partNumber);

        /// <summary>
        /// 强制重新点检。
        /// </summary>
        /// <param name="partNumber">件号</param>
        void ForceRecheck(string partNumber);

        /// <summary>
        /// 设置点检计划（时间/频率）。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="interval">检查间隔</param>
        void SetSchedule(string partNumber, TimeSpan interval);

        /// <summary>点检完成事件</summary>
        event Action<string, CheckResult> OnCheckCompleted;
    }

    /// <summary>
    /// 品质服务接口 —— SPC 品质分析（CPK/PPK/X-R 控制图、箱形图）。
    /// 支持分等级预警停机。
    /// </summary>
    public interface IQualityService
    {
        /// <summary>
        /// 记录测量数据。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <param name="value">测量值</param>
        void RecordMeasurement(string partNumber, string itemName, double value);

        /// <summary>
        /// 计算 SPC 统计量（CPK/PPK/均值/标准差/控制限）。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <returns>SPC 统计结果</returns>
        SpcStatistics CalculateStatistics(string partNumber, string itemName);

        /// <summary>
        /// 获取历史数据点。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <param name="from">起始时间</param>
        /// <param name="to">结束时间</param>
        /// <returns>数据点列表</returns>
        IReadOnlyList<double> GetDataPoints(string partNumber, string itemName, DateTime from, DateTime to);

        /// <summary>
        /// 评估品质等级。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <returns>品质等级</returns>
        QualityGrade EvaluateGrade(string partNumber, string itemName);

        /// <summary>
        /// 检查 SPC 报警（CPK/PPK 偏离、不良连续出现等）。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="itemName">测量项目名称</param>
        /// <returns>报警结果</returns>
        SpcAlarmResult CheckAlarm(string partNumber, string itemName);

        /// <summary>
        /// 清空指定件号的所有测量数据。
        /// </summary>
        /// <param name="partNumber">件号</param>
        void ClearData(string partNumber);
    }

    /// <summary>
    /// 追溯服务接口 —— 工站链式防呆、重码检测、批次对比。
    /// </summary>
    public interface ITraceabilityService
    {
        /// <summary>
        /// 验证产品追溯链。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <param name="partNumber">件号</param>
        /// <param name="stationId">当前工站 ID</param>
        /// <returns>追溯结果</returns>
        TraceResult ValidateProductTrace(string serialNumber, string partNumber, string stationId);

        /// <summary>
        /// 记录工站过站。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <param name="stationId">工站 ID</param>
        /// <param name="operatorId">操作员 ID</param>
        /// <returns>记录是否成功</returns>
        bool RecordStationPass(string serialNumber, string stationId, string operatorId);

        /// <summary>
        /// 获取过站历史。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <returns>过站历史</returns>
        StationHistory GetStationHistory(string serialNumber);

        /// <summary>
        /// 对比批次数量。
        /// </summary>
        /// <param name="batchNumber">批次号</param>
        /// <param name="expectedCount">预期数量</param>
        /// <returns>对比结果</returns>
        BatchCompareResult CompareBatchCounts(string batchNumber, int expectedCount);

        /// <summary>
        /// 判断是否重码。
        /// </summary>
        /// <param name="serialNumber">产品序列号</param>
        /// <param name="stationId">工站 ID</param>
        /// <returns>是否重码</returns>
        bool IsDuplicate(string serialNumber, string stationId);
    }

    /// <summary>
    /// 维保服务接口 —— 自定义维保条目和频率，轴/气动件作业次数追踪。
    /// </summary>
    public interface IMaintenanceService
    {
        /// <summary>
        /// 获取维保计划。
        /// </summary>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>维保项目列表</returns>
        IReadOnlyList<MaintenanceItem> GetPlan(string deviceId);

        /// <summary>
        /// 检查维保项目状态。
        /// </summary>
        /// <param name="itemId">项目 ID</param>
        /// <returns>维保状态</returns>
        MaintenanceStatus CheckItem(string itemId);

        /// <summary>
        /// 记录维保执行。
        /// </summary>
        /// <param name="itemId">项目 ID</param>
        /// <param name="userId">操作员 ID</param>
        /// <param name="notes">备注</param>
        void RecordExecution(string itemId, string userId, string notes);

        /// <summary>
        /// 判断是否超期。
        /// </summary>
        /// <param name="itemId">项目 ID</param>
        /// <returns>是否超期</returns>
        bool IsOverdue(string itemId);

        /// <summary>
        /// 添加自定义维保条目。
        /// </summary>
        /// <param name="itemId">项目 ID</param>
        /// <param name="name">项目名称</param>
        /// <param name="frequency">频率</param>
        /// <param name="countThreshold">次数阈值</param>
        void AddCustomItem(string itemId, string name, MaintenanceFrequency frequency, int countThreshold);

        /// <summary>
        /// 更新轴作业次数。
        /// </summary>
        /// <param name="axisId">轴 ID</param>
        /// <param name="operationCount">作业次数</param>
        void UpdateAxisCount(string axisId, int operationCount);

        /// <summary>
        /// 更新气动件作业次数。
        /// </summary>
        /// <param name="componentId">组件 ID</param>
        /// <param name="operationCount">作业次数</param>
        void UpdatePneumaticCount(string componentId, int operationCount);

        /// <summary>
        /// 获取维保预警列表。
        /// </summary>
        /// <param name="deviceId">设备 ID</param>
        /// <returns>预警列表</returns>
        IReadOnlyList<MaintenanceAlert> GetAlerts(string deviceId);
    }

    /// <summary>
    /// 导出服务接口 —— 按天自动导出生产记录、报警记录、参数等。
    /// 支持离线缓冲，网络恢复后自动补传。
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// 导出生产日报。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>导出结果</returns>
        Task<ExportResult> ExportProductionDaily(DateTime date, string outputPath);

        /// <summary>
        /// 导出报警日报。
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>导出结果</returns>
        Task<ExportResult> ExportAlarmsDaily(DateTime date, string outputPath);

        /// <summary>
        /// 导出参数。
        /// </summary>
        /// <param name="partNumber">件号</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>导出结果</returns>
        Task<ExportResult> ExportParameters(string partNumber, string outputPath);

        /// <summary>网络是否可用</summary>
        bool IsNetworkAvailable { get; }

        /// <summary>获取待导出列表</summary>
        /// <returns>待导出项列表</returns>
        IReadOnlyList<PendingExport> GetPendingExports();

        /// <summary>
        /// 刷新待导出项（网络恢复后自动补传）。
        /// </summary>
        Task FlushPendingExports();
    }
}
