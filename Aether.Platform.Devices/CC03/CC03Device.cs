using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.CC03
{
    /// <summary>干冰清洗设备</summary>
    public class CC03Device : PlcBasedDevice
    {
        public override string DeviceType => "CC03";
        public override string DeviceName => "干冰清洗设备";
        public override string Version => "1.1.0";

        public CC03Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Nozzle"] = "Z", ["Rotary"] = "R", ["Slide"] = "X" },
                OutputIOs = new Dictionary<string, int> { ["DryIceValve"] = 0, ["AirBlow"] = 1, ["Exhaust"] = 2 },
                PlcDRegisters = new Dictionary<string, string> { ["BlastPressure"] = "D800", ["FlowRate"] = "D802", ["Duration"] = "D804" },
                ScannerId = "CC03-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",         Description = "扫码读取件号",             EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",         Description = "X/Z/R轴归零",              EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "LOAD",         Description = "装夹待清洗工件",           EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "MOVE_NOZZLE",  Description = "Z轴移动喷嘴到位",          EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "DRYICE_BLAST", Description = "干冰喷射清洗(旋转覆盖)",   EstimatedDuration = TimeSpan.FromSeconds(8), IsCritical = true },
                new DeviceStep { ActionName = "AIR_BLOW",     Description = "压缩空气吹扫残留",         EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "INSPECT",      Description = "洁净度检查(视觉)",         EstimatedDuration = TimeSpan.FromSeconds(2), IsVerifiable = true },
                new DeviceStep { ActionName = "EXHAUST",      Description = "排风换气",                  EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "HOME_SAFE",    Description = "轴安全归位",                EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "UPLOAD",       Description = "上传清洗数据",              EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
