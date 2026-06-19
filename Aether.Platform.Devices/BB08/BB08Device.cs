using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.BB08
{
    /// <summary>包边设备</summary>
    public class BB08Device : PlcBasedDevice
    {
        public override string DeviceType => "BB08";
        public override string DeviceName => "包边设备";
        public override string Version => "1.4.0";

        public BB08Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Press"] = "Z", ["Rotate"] = "R", ["Feed"] = "Y" },
                OutputIOs = new Dictionary<string, int> { ["Heater"] = 0, ["PressCylinder"] = 1 },
                InputIOs = new Dictionary<string, int> { ["TempSensor"] = 0 },
                PlcDRegisters = new Dictionary<string, string> { ["TargetTemp"] = "D400", ["PressForce"] = "D402", ["PressTime"] = "D404" },
                ScannerId = "BB08-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",        Description = "扫码读取件号",           EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",        Description = "各轴归零",                EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "LOAD",        Description = "装夹镜片+包边材料",      EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "HEAT_UP",     Description = "加热到目标温度",         EstimatedDuration = TimeSpan.FromSeconds(6), IsVerifiable = true },
                new DeviceStep { ActionName = "PRESS_DOWN",  Description = "Z轴下压包边",             EstimatedDuration = TimeSpan.FromSeconds(3), IsCritical = true },
                new DeviceStep { ActionName = "HOLD_PRESS",  Description = "保压冷却",                EstimatedDuration = TimeSpan.FromSeconds(8) },
                new DeviceStep { ActionName = "PRESS_UP",    Description = "Z轴升起",                 EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "INSPECT_EDGE",Description = "包边外观检查",            EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "HOME_SAFE",   Description = "轴安全归位",              EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "UPLOAD",      Description = "上传包边数据",            EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
