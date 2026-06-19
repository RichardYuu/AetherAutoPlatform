namespace Aether.Platform.Devices.Base
{
    /// <summary>基于运动控制卡的设备基类</summary>
    public class DirectControlDevice : DeviceBase
    {
        public override string DeviceType => "DirectControl";
        public override string DeviceName => "Direct Control Device";
        public override string Version => "1.0.0";
    }
}
