using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.DJ10
{
    /// <summary>内外压圈点胶设备</summary>
    public class DJ10Device : DirectControlDevice
    {
        public override string DeviceType => "DJ10";
        public override string DeviceName => "内外压圈点胶设备";
        public override string Version => "2.3.0";

        public DJ10Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["X"] = "X", ["Y"] = "Y", ["Z"] = "Z", ["Dispenser"] = "U" },
                OutputIOs = new Dictionary<string, int> { ["DispenseValve"] = 0, ["UVLight"] = 1, ["Vacuum"] = 2 },
                PlcDRegisters = new Dictionary<string, string> { ["DispenseSpeed"] = "D300", ["DispenseVolume"] = "D302", ["UVTime"] = "D304" },
                ScannerId = "DJ10-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",          Description = "扫码读取件号",         EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME_ALL",      Description = "四轴归零",              EstimatedDuration = TimeSpan.FromSeconds(4) },
                new DeviceStep { ActionName = "LOAD_INNER",    Description = "装夹内压圈",            EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "DISPENSE_INNER",Description = "内压圈点胶(环形路径)",   EstimatedDuration = TimeSpan.FromSeconds(5), IsVerifiable = true },
                new DeviceStep { ActionName = "UV_INNER",      Description = "UV固化(内压圈)",         EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "LOAD_OUTER",    Description = "装夹外压圈",            EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "DISPENSE_OUTER",Description = "外压圈点胶(环形路径)",   EstimatedDuration = TimeSpan.FromSeconds(5), IsVerifiable = true },
                new DeviceStep { ActionName = "UV_OUTER",      Description = "UV固化(外压圈)",         EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "INSPECT",       Description = "胶路检查(视觉)",         EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "HOME_SAFE",     Description = "轴安全归位",             EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "UPLOAD",        Description = "上传点胶数据",           EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
