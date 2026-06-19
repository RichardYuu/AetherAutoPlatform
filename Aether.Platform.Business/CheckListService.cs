using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class CheckListService : ICheckListService
    {
        private readonly ConcurrentDictionary<string, CheckResult> _checkResults = new ConcurrentDictionary<string, CheckResult>();
        private readonly ConcurrentDictionary<string, CheckSchedule> _schedules = new ConcurrentDictionary<string, CheckSchedule>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public event Action<string, CheckResult> OnCheckCompleted;

        public IReadOnlyList<CheckListItem> GetCheckList(string partNumber)
        {
            return new List<CheckListItem>
            {
                new CheckListItem { Id = "CL-001", Name = "安全门状态", Description = "确认所有安全门关闭正常", Category = CheckItemCategory.Safety, IsRequired = true, Result = true },
                new CheckListItem { Id = "CL-002", Name = "急停按钮", Description = "测试急停按钮功能正常", Category = CheckItemCategory.Safety, IsRequired = true, Result = true },
                new CheckListItem { Id = "CL-003", Name = "光栅传感器", Description = "确认光栅区域无遮挡", Category = CheckItemCategory.Safety, IsRequired = true, Result = true },
                new CheckListItem { Id = "CL-004", Name = "气压检测", Description = "检查气源压力 0.45~0.55 MPa", Category = CheckItemCategory.Hardware, IsRequired = true, MinValue = 0.45, MaxValue = 0.55, Unit = "MPa" },
                new CheckListItem { Id = "CL-005", Name = "真空度检测", Description = "检查真空度 -85~-50 kPa", Category = CheckItemCategory.Hardware, IsRequired = true, MinValue = -85, MaxValue = -50, Unit = "kPa" },
                new CheckListItem { Id = "CL-006", Name = "扫码器清洁", Description = "检查扫码器镜头清洁度", Category = CheckItemCategory.Hardware, IsRequired = false },
                new CheckListItem { Id = "CL-007", Name = "视觉标定", Description = "运行视觉系统标定流程", Category = CheckItemCategory.Calibration, IsRequired = true },
                new CheckListItem { Id = "CL-008", Name = "轴回原点", Description = "所有轴执行回原点操作", Category = CheckItemCategory.Calibration, IsRequired = true },
                new CheckListItem { Id = "CL-009", Name = "温度检查", Description = "工作环境温度 20~28 ℃", Category = CheckItemCategory.Environment, IsRequired = false, MinValue = 20, MaxValue = 28, Unit = "℃" },
                new CheckListItem { Id = "CL-010", Name = "辅料有效期", Description = "检查胶水/载具等辅料有效期", Category = CheckItemCategory.Consumable, IsRequired = true },
                new CheckListItem { Id = "CL-011", Name = "料仓复位", Description = "确认料仓处于初始位置", Category = CheckItemCategory.Hardware, IsRequired = true },
                new CheckListItem { Id = "CL-012", Name = "气缸动作", Description = "测试所有气缸伸缩正常", Category = CheckItemCategory.Hardware, IsRequired = true },
            };
        }

        public CheckResult Check(string partNumber, string userId)
        {
            var items = GetCheckList(partNumber).ToList();
            var passed = new Random().NextDouble() > 0.15;
            int passedCount = 0;
            int failedCount = 0;

            foreach (var item in items)
            {
                item.IsChecked = true;
                if (item.MinValue != 0 || item.MaxValue != 0)
                {
                    var range = item.MaxValue - item.MinValue;
                    item.MeasuredValue = Math.Round(item.MinValue + new Random().NextDouble() * range, 2);
                    item.Result = item.MeasuredValue >= item.MinValue && item.MeasuredValue <= item.MaxValue;
                }
                else
                {
                    item.Result = passed || !item.IsRequired;
                }

                if (item.Result) passedCount++;
                else failedCount++;
            }

            var result = new CheckResult
            {
                AllPassed = failedCount == 0,
                PartNumber = partNumber,
                UserId = userId,
                CheckTime = DateTime.Now,
                Items = items,
                PassedCount = passedCount,
                FailedCount = failedCount
            };

            _checkResults[partNumber] = result;

            var schedule = new CheckSchedule
            {
                PartNumber = partNumber,
                Interval = TimeSpan.FromHours(8),
                LastCheckTime = DateTime.Now,
                NextCheckTime = DateTime.Now.AddHours(8)
            };
            _schedules[partNumber] = schedule;

            OnCheckCompleted?.Invoke(partNumber, result);
            return result;
        }

        public bool IsChecked(string partNumber)
        {
            if (!_schedules.TryGetValue(partNumber, out var schedule))
                return false;

            return !schedule.IsOverdue;
        }

        public void ForceRecheck(string partNumber)
        {
            _checkResults.TryRemove(partNumber, out _);
            if (_schedules.TryGetValue(partNumber, out var schedule))
            {
                schedule.NextCheckTime = DateTime.Now;
            }
        }

        public void SetSchedule(string partNumber, TimeSpan interval)
        {
            _schedules[partNumber] = new CheckSchedule
            {
                PartNumber = partNumber,
                Interval = interval,
                LastCheckTime = DateTime.Now,
                NextCheckTime = DateTime.Now.Add(interval)
            };
        }
    }
}
