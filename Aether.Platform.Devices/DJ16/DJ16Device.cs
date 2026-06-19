using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.DJ16
{
    /// <summary>901点胶包边设备</summary>
    public class DJ16Device : PlcBasedDevice
    {
        public override string DeviceType => "DJ16";
        public override string DeviceName => "901点胶包边设备";
        public override string Version => "1.3.0";

        public DJ16Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["X"] = "X", ["Y"] = "Y", ["Z"] = "Z", ["Dispenser"] = "U", ["Rotate"] = "R" },
                OutputIOs = new Dictionary<string, int> { ["DispenseValve"] = 0, ["UV"] = 1, ["Heater"] = 2 },
                PlcDRegisters = new Dictionary<string, string> { ["DispenseVol"] = "D500", ["UVTime"] = "D502", ["PressTemp"] = "D504" },
                ScannerId = "DJ16-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",           Description = "扫码读取件号",           EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME_ALL",       Description = "五轴归零",                EstimatedDuration = TimeSpan.FromSeconds(4) },
                new DeviceStep { ActionName = "LOAD",           Description = "装夹镜片+压圈+包边料",  EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "DISPENSE_901",   Description = "901胶点胶(环形路径)",    EstimatedDuration = TimeSpan.FromSeconds(5), IsVerifiable = true },
                new DeviceStep { ActionName = "UV_CURE",        Description = "UV固化",                  EstimatedDuration = TimeSpan.FromSeconds(4) },
                new DeviceStep { ActionName = "HEAT_PRESS",     Description = "加热压包边",              EstimatedDuration = TimeSpan.FromSeconds(10), IsCritical = true },
                new DeviceStep { ActionName = "COOL_DOWN",      Description = "冷却定型",                EstimatedDuration = TimeSpan.FromSeconds(5) },
                new DeviceStep { ActionName = "INSPECT",        Description = "外观/胶路检查",           EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "HOME_SAFE",      Description = "轴安全归位",              EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "UPLOAD",         Description = "上传数据",                EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
