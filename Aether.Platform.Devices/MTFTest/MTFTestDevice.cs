using System;
using System.Collections.Generic;
using Aether.Platform.Devices.Base;

namespace Aether.Platform.Devices.MTFTest
{
    /// <summary>MTF测试设备</summary>
    public class MTFTestDevice : PlcBasedDevice
    {
        public override string DeviceType => "MTFTest";
        public override string DeviceName => "MTF测试设备";
        public override string Version => "2.1.0";

        public MTFTestDevice()
        {
            HalConfig = new HalBinding
            {
                Axes = new Dictionary<string, string> { ["Focus"] = "Z", ["Turret"] = "R" },
                OutputIOs = new Dictionary<string, int> { ["LightSource"] = 0, ["ChartSelect"] = 1 },
                CameraId = "MTF-CAM-01",
                ScannerId = "MTF-SCAN-01",
                PlcDRegisters = new Dictionary<string, string> { ["ExposureTime"] = "D100", ["Gain"] = "D102" },
            };

            _steps = new List<DeviceStep>
            {
                new DeviceStep { ActionName = "SCAN_BARCODE",    Description = "扫码读取件号",      EstimatedDuration = TimeSpan.FromSeconds(1.5), IsVerifiable = true },
                new DeviceStep { ActionName = "LOAD_LENS",       Description = "装夹被测镜头",      EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "MOVE_TO_HOME",    Description = "轴归零",             EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "TURN_ON_LIGHT",   Description = "打开光源",           EstimatedDuration = TimeSpan.FromMilliseconds(500) },
                new DeviceStep { ActionName = "AUTO_FOCUS",      Description = "自动对焦(清晰度搜索)", EstimatedDuration = TimeSpan.FromSeconds(8), IsVerifiable = true },
                new DeviceStep { ActionName = "SET_EXPOSURE",    Description = "设置曝光参数",      EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "CAPTURE_MTF",     Description = "采集MTF图像",       EstimatedDuration = TimeSpan.FromSeconds(3), IsVerifiable = true },
                new DeviceStep { ActionName = "ANALYZE_MTF",     Description = "MTF数值计算(中心/边缘)", EstimatedDuration = TimeSpan.FromSeconds(1.5) },
                new DeviceStep { ActionName = "TURN_OFF_LIGHT",  Description = "关闭光源",           EstimatedDuration = TimeSpan.FromMilliseconds(300) },
                new DeviceStep { ActionName = "RETURN_HOME",     Description = "轴归位",             EstimatedDuration = TimeSpan.FromSeconds(3) },
                new DeviceStep { ActionName = "UPLOAD_RESULT",   Description = "上传测试结果到MES", EstimatedDuration = TimeSpan.FromSeconds(2) },
                new DeviceStep { ActionName = "RELEASE_LENS",    Description = "释放镜头",           EstimatedDuration = TimeSpan.FromSeconds(1) },
            };
        }
    }
}
