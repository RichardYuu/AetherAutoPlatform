using System;
using System.Collections.Generic;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Services
{
    public class FlowPropertyService : IFlowPropertyService
    {
        private readonly Dictionary<int, FlowAction> _actions = new Dictionary<int, FlowAction>();

        public IReadOnlyDictionary<int, FlowAction> GetAllActions() => new Dictionary<int, FlowAction>(_actions);
        public FlowAction GetAction(int code) => _actions.TryGetValue(code, out var action) ? action : null;
        public bool IsDebugAction(int code) => code >= 100 && code < 200;
        public bool IsAutoAction(int code) => code >= 300;

        public void RegisterAction(FlowAction action)
        {
            if (action != null) _actions[action.Code] = action;
        }

        public void RegisterActions(IEnumerable<FlowAction> actions)
        {
            foreach (var action in actions) RegisterAction(action);
        }
    }
}
