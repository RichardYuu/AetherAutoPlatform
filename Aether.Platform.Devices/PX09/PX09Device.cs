using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.PX09
{
    /// <summary>偏心测试设备</summary>
    public class PX09Device : PlcBasedDevice
    {
        public override string DeviceType => "PX09";
        public override string DeviceName => "偏心测试设备";
        public override string Version => "1.8.0";

        public PX09Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Rotary"] = "R", ["Focus"] = "Z" },
                OutputIOs = new Dictionary<string, int> { ["LightSource"] = 0, ["AlignLaser"] = 1 },
                CameraId = "PX09-CAM-01",
                ScannerId = "PX09-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",        Description = "扫码读取件号",        EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",        Description = "R/Z轴归零",           EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "LOAD",        Description = "装夹待测镜片",        EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "LIGHT_ON",    Description = "打开光源+对准激光",  EstimatedDuration = TimeSpan.FromMilliseconds(500) },
                new DeviceStep { ActionName = "ROTATE_360",  Description = "R轴旋转360°采集",     EstimatedDuration = TimeSpan.FromSeconds(8), IsVerifiable = true },
                new DeviceStep { ActionName = "CALC_ECCENTRICITY", Description = "计算偏心量(中心/倾斜)", EstimatedDuration = TimeSpan.FromSeconds(1) },
                new DeviceStep { ActionName = "LIGHT_OFF",   Description = "关闭光源",             EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "HOME_SAFE",   Description = "轴安全归位",           EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "UPLOAD",      Description = "上传结果",              EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
