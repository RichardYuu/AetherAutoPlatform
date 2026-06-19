using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ConcurrentDictionary<string, MaintenanceItem> _items = new ConcurrentDictionary<string, MaintenanceItem>();
        private readonly ConcurrentDictionary<string, int> _axisCounts = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _pneumaticCounts = new ConcurrentDictionary<string, int>();
        private readonly List<MaintenanceAlert> _alerts = new List<MaintenanceAlert>();
        private readonly IMaintenanceRecordRepository _repository;

        public MaintenanceService(IMaintenanceRecordRepository repository = null)
        {
            _repository = repository;
            if (_repository != null)
            {
                try
                {
                    var saved = _repository.LoadPlanAsync().Result;
                    if (saved != null && saved.Count > 0)
                    {
                        foreach (var item in saved) _items[item.Id] = item;
                        return;
                    }
                }
                catch { }
            }
            InitializeDefaultPlan();
        }

        private void InitializeDefaultPlan()
        {
            var defaultItems = new[]
            {
                new MaintenanceItem { Id = "MAINT-001", Name = "轴导轨润滑", Description = "对所有运动轴导轨加注润滑脂", Frequency = MaintenanceFrequency.Weekly, NextDueDate = DateTime.Now.AddDays(7) },
                new MaintenanceItem { Id = "MAINT-002", Name = "气缸密封圈检查", Description = "检查所有气缸密封圈磨损情况", Frequency = MaintenanceFrequency.Monthly, NextDueDate = DateTime.Now.AddDays(30) },
                new MaintenanceItem { Id = "MAINT-003", Name = "真空泵保养", Description = "清洁真空泵滤芯，检查真空度", Frequency = MaintenanceFrequency.Monthly, NextDueDate = DateTime.Now.AddDays(30) },
                new MaintenanceItem { Id = "MAINT-004", Name = "扫码器清洁", Description = "清洁扫码器镜片和反射面", Frequency = MaintenanceFrequency.Daily, NextDueDate = DateTime.Now.AddDays(1) },
                new MaintenanceItem { Id = "MAINT-005", Name = "视觉系统校准", Description = "运行视觉标定程序", Frequency = MaintenanceFrequency.Weekly, NextDueDate = DateTime.Now.AddDays(7) },
                new MaintenanceItem { Id = "MAINT-006", Name = "料仓传感器检测", Description = "测试料仓定位传感器灵敏度", Frequency = MaintenanceFrequency.Monthly, NextDueDate = DateTime.Now.AddDays(30) },
                new MaintenanceItem { Id = "MAINT-007", Name = "电气柜除尘", Description = "清理电气柜内部积尘", Frequency = MaintenanceFrequency.Quarterly, NextDueDate = DateTime.Now.AddDays(90) },
                new MaintenanceItem { Id = "MAINT-008", Name = "UPS电池检测", Description = "检测UPS电池电压和容量", Frequency = MaintenanceFrequency.Monthly, NextDueDate = DateTime.Now.AddDays(30) },
                new MaintenanceItem { Id = "MAINT-009", Name = "急停回路测试", Description = "测试所有急停按钮和安全门回路", Frequency = MaintenanceFrequency.Monthly, NextDueDate = DateTime.Now.AddDays(30) },
                new MaintenanceItem { Id = "MAINT-010", Name = "气源三联件排水", Description = "排放气源三联件积水", Frequency = MaintenanceFrequency.Daily, NextDueDate = DateTime.Now.AddDays(1) },
            };

            foreach (var item in defaultItems)
                _items[item.Id] = item;
        }

        public IReadOnlyList<MaintenanceItem> GetPlan(string deviceId)
        {
            return _items.Values.OrderBy(i => i.NextDueDate).ToList().AsReadOnly();
        }

        public MaintenanceStatus CheckItem(string itemId)
        {
            if (!_items.TryGetValue(itemId, out var item))
                return new MaintenanceStatus { ItemId = itemId };

            var status = new MaintenanceStatus
            {
                ItemId = itemId,
                IsOverdue = item.IsOverdue,
                RemainingTime = item.NextDueDate - DateTime.Now,
            };

            if (item.Frequency == MaintenanceFrequency.ByOperationCount)
            {
                status.RemainingCount = item.CountThreshold - item.CurrentCount;
                status.NeedsAttention = status.RemainingCount <= item.CountThreshold * 0.2;
            }
            else
            {
                status.NeedsAttention = status.RemainingTime.TotalHours <= 24;
            }

            return status;
        }

        public bool IsOverdue(string itemId)
        {
            if (!_items.TryGetValue(itemId, out var item))
                return false;
            return item.IsOverdue;
        }

        public void RecordExecution(string itemId, string userId, string notes)
        {
            if (!_items.TryGetValue(itemId, out var item))
                return;

            item.LastExecution = DateTime.Now;
            item.LastOperator = userId;

            switch (item.Frequency)
            {
                case MaintenanceFrequency.Daily: item.NextDueDate = DateTime.Now.AddDays(1); break;
                case MaintenanceFrequency.Weekly: item.NextDueDate = DateTime.Now.AddDays(7); break;
                case MaintenanceFrequency.Monthly: item.NextDueDate = DateTime.Now.AddDays(30); break;
                case MaintenanceFrequency.Quarterly: item.NextDueDate = DateTime.Now.AddDays(90); break;
                default: item.NextDueDate = DateTime.Now.AddDays(30); break;
            }

            _alerts.RemoveAll(a => a.ItemId == itemId);

            SavePlanToRepository();
        }

        public void AddCustomItem(string itemId, string name, MaintenanceFrequency frequency, int countThreshold)
        {
            _items[itemId] = new MaintenanceItem
            {
                Id = itemId,
                Name = name,
                Frequency = frequency,
                CountThreshold = countThreshold,
                NextDueDate = DateTime.Now.AddDays(frequency == MaintenanceFrequency.Daily ? 1 : 7)
            };
            SavePlanToRepository();
        }

        private void SavePlanToRepository()
        {
            if (_repository != null)
            {
                try { _repository.SavePlanAsync(_items.Values.ToList()); }
                catch { }
            }
        }

        public void UpdateAxisCount(string axisId, int operationCount)
        {
            _axisCounts.AddOrUpdate(axisId, operationCount, (_, old) => old + operationCount);
        }

        public void UpdatePneumaticCount(string componentId, int operationCount)
        {
            _pneumaticCounts.AddOrUpdate(componentId, operationCount, (_, old) => old + operationCount);
        }

        public IReadOnlyList<MaintenanceAlert> GetAlerts(string deviceId)
        {
            var alerts = new List<MaintenanceAlert>();

            foreach (var item in _items.Values)
            {
                var status = CheckItem(item.Id);
                if (status.IsOverdue)
                {
                    alerts.Add(new MaintenanceAlert
                    {
                        ItemId = item.Id,
                        ItemName = item.Name,
                        Level = MaintenanceAlertLevel.Critical,
                        Message = $"{item.Name} 已逾期 {(DateTime.Now - item.NextDueDate).TotalHours:F0} 小时",
                        AlertTime = DateTime.Now
                    });
                }
                else if (status.NeedsAttention)
                {
                    alerts.Add(new MaintenanceAlert
                    {
                        ItemId = item.Id,
                        ItemName = item.Name,
                        Level = MaintenanceAlertLevel.Warning,
                        Message = $"{item.Name} 即将到期: 剩余 {status.RemainingTime.TotalHours:F1} 小时",
                        AlertTime = DateTime.Now
                    });
                }
            }

            return alerts.AsReadOnly();
        }
    }
}
