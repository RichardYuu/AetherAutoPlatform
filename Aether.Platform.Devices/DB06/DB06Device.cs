using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.DB06
{
    /// <summary>自动打标设备</summary>
    public class DB06Device : DirectControlDevice
    {
        public override string DeviceType => "DB06";
        public override string DeviceName => "自动打标设备";
        public override string Version => "1.2.0";

        public DB06Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Laser"] = "X", ["Y"] = "Y", ["Focus"] = "Z" },
                OutputIOs = new Dictionary<string, int> { ["LaserPower"] = 0, ["FumeExtractor"] = 1 },
                PlcDRegisters = new Dictionary<string, string> { ["LaserPowerLevel"] = "D700", ["MarkSpeed"] = "D702", ["PulseFreq"] = "D704" },
                ScannerId = "DB06-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",        Description = "扫码读取件号(获取打标内容)", EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",        Description = "X/Y/Z轴归零",               EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "LOAD",        Description = "装夹工件",                   EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "FOCUS",       Description = "Z轴对焦到打标面",           EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "LASER_MARK",  Description = "激光打标(二维码+文字)",     EstimatedDuration = TimeSpan.FromSeconds(3), IsCritical = true },
                new DeviceStep { ActionName = "VERIFY_MARK", Description = "扫码验证打标内容",           EstimatedDuration = TimeSpan.FromSeconds(1.5), IsVerifiable = true },
                new DeviceStep { ActionName = "EXTRACT_FUME",Description = "排烟除尘",                   EstimatedDuration = TimeSpan.FromSeconds(1) },
                new DeviceStep { ActionName = "HOME_SAFE",   Description = "轴安全归位",                 EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "UPLOAD",      Description = "上传打标记录",               EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
