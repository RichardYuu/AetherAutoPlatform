using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Devices.Base
{
    public abstract class DeviceBase : IDevice
    {
        protected List<DeviceStep> _steps = new List<DeviceStep>();
        protected int _currentStepIndex = -1;
        protected DeviceStatus _status = DeviceStatus.Idle;
        protected CancellationTokenSource _runCts;
        protected Dictionary<string, object> _runData = new Dictionary<string, object>();

        public abstract string DeviceType { get; }
        public abstract string DeviceName { get; }
        public abstract string Version { get; }

        /// <summary>HAL 绑定配置（子类覆盖）</summary>
        public virtual HalBinding HalConfig { get; protected set; } = new HalBinding();

        /// <summary>工步列表（子类覆盖）</summary>
        public virtual IReadOnlyList<DeviceStep> Steps => _steps;

        /// <summary>当前执行到第几步</summary>
        public int CurrentStepIndex => _currentStepIndex;

        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event Action<string, DeviceStep> OnStepStarted;
        public event Action<string, DeviceStep, bool> OnStepCompleted;

        protected void OnStatusChanged(DeviceStatus newStatus)
        {
            _status = newStatus;
            StatusChanged?.Invoke(this, new StatusChangedEventArgs { NewStatus = newStatus });
        }

        // ---- IDevice ----

        public virtual bool Initialize()
        {
            _currentStepIndex = -1;
            _runData.Clear();
            OnStatusChanged(DeviceStatus.Idle);
            return true;
        }

        public virtual bool Start()
        {
            if (_status == DeviceStatus.Running) return false;
            OnStatusChanged(DeviceStatus.Running);
            _currentStepIndex = -1;
            _runCts = new CancellationTokenSource();
            // 异步运行工步序列
            Task.Run(() => RunStepsAsync(_runCts.Token));
            return true;
        }

        public virtual bool Stop()
        {
            _runCts?.Cancel();
            OnStatusChanged(DeviceStatus.Idle);
            return true;
        }

        public virtual bool Shutdown()
        {
            _runCts?.Cancel();
            OnStatusChanged(DeviceStatus.Idle);
            return true;
        }

        public virtual DeviceStatus GetStatus() => _status;

        public virtual DeviceParameters GetParameters()
        {
            return new DeviceParameters
            {
                ProcessParams = new Dictionary<string, object>
                {
                    ["DeviceType"] = DeviceType,
                    ["DeviceName"] = DeviceName,
                    ["StepCount"] = _steps.Count,
                }
            };
        }

        public virtual bool SetParameters(DeviceParameters parameters) => true;

        public virtual bool ExecuteAction(string actionName, Dictionary<string, object> parameters)
        {
            return SingleStepExecute(actionName);
        }

        public virtual Dictionary<string, object> GetTestData() => new Dictionary<string, object>(_runData);

        public virtual List<string> GetAvailableActions()
        {
            return _steps.Select(s => s.ActionName).ToList();
        }

        public virtual bool SingleStepExecute(string action)
        {
            var step = _steps.FirstOrDefault(s => s.ActionName == action);
            if (step == null) return false;
            _runData[$"Step_{step.ActionName}_Time"] = DateTime.Now;
            return true;
        }

        // ---- 工步序列执行 ----

        protected virtual async Task RunStepsAsync(CancellationToken ct)
        {
            for (_currentStepIndex = 0; _currentStepIndex < _steps.Count; _currentStepIndex++)
            {
                ct.ThrowIfCancellationRequested();
                var step = _steps[_currentStepIndex];

                OnStepStarted?.Invoke(DeviceType, step);
                _runData[$"Step_{step.ActionName}_Start"] = DateTime.Now;

                try
                {
                    bool ok = await ExecuteStepAsync(step, ct);
                    OnStepCompleted?.Invoke(DeviceType, step, ok);
                    _runData[$"Step_{step.ActionName}_Result"] = ok ? "OK" : "NG";

                    if (!ok && step.IsCritical)
                    {
                        OnStatusChanged(DeviceStatus.Error);
                        return;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    if (step.IsCritical)
                    {
                        OnStatusChanged(DeviceStatus.Error);
                        _runData[$"Step_{step.ActionName}_Error"] = ex.Message;
                        return;
                    }
                }
            }
            OnStatusChanged(DeviceStatus.Idle);
        }

        /// <summary>子类可覆盖以实现具体工步的硬件调用</summary>
        protected virtual async Task<bool> ExecuteStepAsync(DeviceStep step, CancellationToken ct)
        {
            // 默认: 模拟等待工步预估时长
            await Task.Delay(step.EstimatedDuration, ct);
            return true;
        }
    }
}
