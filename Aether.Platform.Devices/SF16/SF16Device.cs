using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.SF16
{
    /// <summary>内压圈锁付设备</summary>
    public class SF16Device : PlcBasedDevice
    {
        public override string DeviceType => "SF16";
        public override string DeviceName => "内压圈锁付设备";
        public override string Version => "1.7.0";

        public SF16Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Screw"] = "Z", ["Rotate"] = "R", ["Lift"] = "Y" },
                OutputIOs = new Dictionary<string, int> { ["ScrewMotor"] = 0, ["TorqueClutch"] = 1 },
                InputIOs = new Dictionary<string, int> { ["TorqueSensor"] = 0, ["DepthSensor"] = 1 },
                PlcDRegisters = new Dictionary<string, string> { ["TargetTorque"] = "D600", ["ScrewDepth"] = "D602", ["Speed"] = "D604" },
                ScannerId = "SF16-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",       Description = "扫码读取件号",          EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "HOME",       Description = "Z/R/Y轴归零",           EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "LOAD",       Description = "装夹内压圈+螺丝",       EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "ALIGN",      Description = "R轴旋转对位螺丝孔",    EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "SCREW_DOWN", Description = "Z轴旋入锁付(扭矩控制)", EstimatedDuration = TimeSpan.FromSeconds(4), IsCritical = true, IsVerifiable = true },
                new DeviceStep { ActionName = "CHECK_TORQUE",Description = "验证锁付扭矩",          EstimatedDuration = TimeSpan.FromMilliseconds(500), IsVerifiable = true },
                new DeviceStep { ActionName = "LIFT",       Description = "Y轴升起",               EstimatedDuration = TimeSpan.FromSeconds(1) },
                new DeviceStep { ActionName = "HOME_SAFE",  Description = "轴安全归位",             EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "UPLOAD",     Description = "上传锁付数据",          EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
