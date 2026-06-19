using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.ZZ14
{
    /// <summary>IR组立点胶设备</summary>
    public class ZZ14Device : PlcBasedDevice
    {
        public override string DeviceType => "ZZ14";
        public override string DeviceName => "IR组立点胶设备";
        public override string Version => "1.6.0";

        public ZZ14Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["X"] = "X", ["Y"] = "Y", ["Z"] = "Z", ["Rotary"] = "R" },
                OutputIOs = new Dictionary<string, int> { ["Dispense"] = 0, ["UV"] = 1, ["Gripper"] = 2 },
                CameraId = "ZZ14-CAM-01",
                ScannerId = "ZZ14-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",       Description = "扫码读取件号",       EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",       Description = "各轴归零",            EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "PICK_IR",    Description = "夹取IR片",            EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "ALIGN_IR",   Description = "视觉对位IR片",        EstimatedDuration = TimeSpan.FromSeconds(2), IsVerifiable = true },
                new DeviceStep { ActionName = "PLACE_IR",   Description = "放置IR片到镜筒",     EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "DISPENSE",   Description = "点胶(4点对称)",       EstimatedDuration = TimeSpan.FromSeconds(4), IsVerifiable = true },
                new DeviceStep { ActionName = "UV_CURE",    Description = "UV固化",               EstimatedDuration = TimeSpan.FromSeconds(5) },
                new DeviceStep { ActionName = "INSPECT",    Description = "胶点检查",             EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME_SAFE",  Description = "轴安全归位",           EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "UPLOAD",     Description = "上传组立数据",         EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
