namespace Aether.Platform.Devices.Base
{
    /// <summary>基于 PLC 的设备基类</summary>
    public class PlcBasedDevice : DeviceBase
    {
        public override string DeviceType => "PlcBased";
        public override string DeviceName => "PLC Controlled Device";
        public override string Version => "1.0.0";
    }
}
