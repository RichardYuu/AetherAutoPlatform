using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.AssemblyNew
{
    /// <summary>组装设备(新领域)</summary>
    public class AssemblyNewDevice : PlcBasedDevice
    {
        public override string DeviceType => "AssemblyNew";
        public override string DeviceName => "组装设备(新领域)";
        public override string Version => "3.0.0";

        public AssemblyNewDevice()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["PickX"] = "X", ["PickY"] = "Y", ["PickZ"] = "Z", ["Rotary"] = "R", ["Press"] = "U" },
                OutputIOs = new Dictionary<string, int> { ["Vacuum1"] = 0, ["Vacuum2"] = 1, ["PressCyl"] = 2, ["Dispense"] = 3 },
                InputIOs = new Dictionary<string, int> { ["PartPresent1"] = 0, ["PartPresent2"] = 1 },
                CameraId = "ASM-CAM-01",
                ScannerId = "ASM-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN_TRAY",     Description = "扫描料盘条码",              EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME_ALL",      Description = "五轴归零",                   EstimatedDuration = TimeSpan.FromSeconds(4) },
                new DeviceStep { ActionName = "PICK_LENS1",    Description = "吸取镜片1(真空吸嘴)",       EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "VISION_ALIGN1", Description = "视觉定位镜片1位置",          EstimatedDuration = TimeSpan.FromSeconds(1.5), IsVerifiable = true },
                new DeviceStep { ActionName = "PLACE_LENS1",   Description = "放置镜片1到组装工位",       EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "PICK_LENS2",    Description = "吸取镜片2",                  EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "VISION_ALIGN2", Description = "视觉定位镜片2位置",          EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "PLACE_LENS2",   Description = "放置镜片2",                  EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "DISPENSE",      Description = "点胶(多点精确控制)",         EstimatedDuration = TimeSpan.FromSeconds(5), IsVerifiable = true },
                new DeviceStep { ActionName = "PRESS_ASSEMBLE",Description = "U轴压合组装",                 EstimatedDuration = TimeSpan.FromSeconds(3), IsCritical = true },
                new DeviceStep { ActionName = "UV_CURE",       Description = "UV固化",                      EstimatedDuration = TimeSpan.FromSeconds(6) },
                new DeviceStep { ActionName = "INSPECT",       Description = "组装精度视觉检查",           EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "UNLOAD",        Description = "取出成品到料盘",             EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "HOME_SAFE",     Description = "轴安全归位",                  EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "UPLOAD",        Description = "上传组装数据到MES",           EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
