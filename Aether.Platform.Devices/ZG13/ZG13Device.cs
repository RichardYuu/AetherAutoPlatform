using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.ZG13
{
    /// <summary>杂光检测设备</summary>
    public class ZG13Device : PlcBasedDevice
    {
        public override string DeviceType => "ZG13";
        public override string DeviceName => "杂光检测设备";
        public override string Version => "1.5.0";

        public ZG13Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["ZAxis"] = "Z", ["RAxis"] = "R" },
                OutputIOs = new Dictionary<string, int> { ["Light"] = 0, ["Shutter"] = 1 },
                CameraId = "ZG13-CAM-01",
                ScannerId = "ZG13-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",       Description = "扫码读取条码",     EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",       Description = "Z/R 轴归零",       EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "LIGHT_ON",   Description = "打开杂光光源",     EstimatedDuration = TimeSpan.FromMilliseconds(500) },
                new DeviceStep { ActionName = "MOVE_Z",     Description = "Z轴移动到检测位",  EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "ROTATE_R",   Description = "R轴旋转(0°/90°/180°/270°)", EstimatedDuration = TimeSpan.FromSeconds(6) },
                new DeviceStep { ActionName = "CAPTURE",    Description = "四角度采集杂光图像", EstimatedDuration = TimeSpan.FromSeconds(4), IsVerifiable = true },
                new DeviceStep { ActionName = "ANALYZE",    Description = "杂光分析(光斑/鬼影/散射)", EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "LIGHT_OFF",  Description = "关闭光源",          EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "HOME_SAFE",  Description = "轴安全归位",        EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "UPLOAD",     Description = "上传检测结果",      EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
