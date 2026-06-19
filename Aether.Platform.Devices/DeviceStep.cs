using System;
using System.Collections.Generic;

namespace Aether.Platform.Devices
{
    /// <summary>设备工步描述</summary>
    public class DeviceStep
    {
        public string ActionName { get; set; }
        public string Description { get; set; }
        public TimeSpan EstimatedDuration { get; set; } = TimeSpan.FromSeconds(2);
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsCritical { get; set; } = true;   // 关键工步失败则终止流程
        public bool IsVerifiable { get; set; }           // 是否可用传感器验证
    }
}
