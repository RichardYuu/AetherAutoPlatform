using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.XL07
{
    /// <summary>泄露测试设备</summary>
    public class XL07Device : PlcBasedDevice
    {
        public override string DeviceType => "XL07";
        public override string DeviceName => "泄露测试设备";
        public override string Version => "2.0.0";

        public XL07Device()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Seal"] = "Z", ["Lift"] = "Y" },
                OutputIOs = new Dictionary<string, int> { ["VacuumValve"] = 0, ["PressureValve"] = 1, ["ReleaseValve"] = 2 },
                InputIOs = new Dictionary<string, int> { ["SealSensor"] = 0, ["PressureSensor"] = 1 },
                PlcDRegisters = new Dictionary<string, string> { ["TargetPressure"] = "D200", ["LeakThreshold"] = "D202", ["TestTime"] = "D204" },
                ScannerId = "XL07-SCAN-01",
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN",           Description = "扫码读取件号",        EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "LOAD_PART",      Description = "装夹工件",             EstimatedDuration = TimeSpan.FromSeconds(2.5) },
                new DeviceStep { ActionName = "SEAL_CLOSE",     Description = "Z轴下压密封",          EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "CHECK_SEAL",     Description = "检查密封状态(传感器)", EstimatedDuration = TimeSpan.FromMilliseconds(500), IsVerifiable = true },
                new DeviceStep { ActionName = "EVACUATE",       Description = "真空泵抽气",           EstimatedDuration = TimeSpan.FromSeconds(5) },
                new DeviceStep { ActionName = "STABILIZE",      Description = "压力稳定等待",         EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "LEAK_TEST",      Description = "保压泄漏测试(差压法)", EstimatedDuration = TimeSpan.FromSeconds(10), IsVerifiable = true, IsCritical = true },
                new DeviceStep { ActionName = "RELEASE_VACUUM", Description = "释放真空",             EstimatedDuration = TimeSpan.FromSeconds(1) },
                new DeviceStep { ActionName = "SEAL_RAISE",     Description = "Y轴升起释放工件",      EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "JUDGE",          Description = "判定合格/不合格",      EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "UPLOAD",         Description = "上传到IFMS/MES",       EstimatedDuration = TimeSpan.FromSeconds(1.5) },
            };
        }
    }
}
