using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.Assembly
{
    /// <summary>组装机(常规)</summary>
    public class AssemblyDevice : DirectControlDevice
    {
        public override string DeviceType => "Assembly";
        public override string DeviceName => "组装机(常规)";
        public override string Version => "2.5.0";

        public AssemblyDevice()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["X"] = "X", ["Y"] = "Y", ["Z"] = "Z", ["Press"] = "U" },
                OutputIOs = new Dictionary<string, int> { ["Gripper"] = 0, ["PressCyl"] = 1 },
                InputIOs = new Dictionary<string, int> { ["PartReady"] = 0 },
                CameraId = "ASM-CAM-02",
                ScannerId = "ASM-SCAN-02",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN_PART",    Description = "扫码读取件号",             EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",         Description = "四轴归零",                  EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "PICK_PART",    Description = "夹取工件",                  EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "ALIGN_PART",   Description = "视觉对位工件",              EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "PLACE_PART",   Description = "放置工件到组装位",          EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "PRESS_FIT",    Description = "U轴压装",                   EstimatedDuration = TimeSpan.FromSeconds(3), IsCritical = true },
                new DeviceStep { ActionName = "CHECK_DEPTH",  Description = "检查压入深度",              EstimatedDuration = TimeSpan.FromMilliseconds(500), IsVerifiable = true },
                new DeviceStep { ActionName = "RELEASE",      Description = "释放夹爪",                  EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "HOME_SAFE",    Description = "轴安全归位",                EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "UPLOAD",       Description = "上传组装数据",              EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
