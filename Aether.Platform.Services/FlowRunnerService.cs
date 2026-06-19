using System;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;
using Aether.Platform.Core.Utilities;

namespace Aether.Platform.Services
{
    public class FlowRunnerService : IFlowRunner
    {
        private readonly IFlowPropertyService _flowProps;
        private CancellationTokenSource _cts;
        private FlowAction _currentAction;

        public FlowState CurrentState { get; private set; } = FlowState.Idle;
        public int CurrentStepCode { get; private set; }

        public event Action<FlowState> OnStateChanged;
        public event Action<int, string> OnStepChanged;

        public FlowRunnerService()
        {
            try { _flowProps = ServiceLocator.GetService<IFlowPropertyService>(); }
            catch { _flowProps = new FlowPropertyService(); }
        }

        private void SetState(FlowState state) { CurrentState = state; OnStateChanged?.Invoke(state); }

        public async Task StartAsync(int startCode, CancellationToken ct)
        {
            SetState(FlowState.Running);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            CurrentStepCode = startCode;
            _currentAction = _flowProps?.GetAction(startCode);

            if (_currentAction != null)
            {
                OnStepChanged?.Invoke(startCode, _currentAction.Name);
                try { await _currentAction.Execute(_cts.Token); }
                catch (OperationCanceledException) { }
                catch (Exception) { SetState(FlowState.Error); return; }
            }
            SetState(FlowState.Idle);
        }

        public Task PauseAsync() { SetState(FlowState.Paused); return Task.CompletedTask; }
        public Task ResumeAsync() { SetState(FlowState.Running); return Task.CompletedTask; }
        public Task StopAsync() { _cts?.Cancel(); SetState(FlowState.Idle); return Task.CompletedTask; }
        public Task EmergencyStopAsync() { _cts?.Cancel(); SetState(FlowState.Idle); return Task.CompletedTask; }
        public Task ResetAsync() { CurrentStepCode = 0; SetState(FlowState.Idle); return Task.CompletedTask; }
    }
}
