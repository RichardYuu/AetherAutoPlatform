using System.Collections.Generic;

namespace Aether.Platform.Devices
{
    /// <summary>设备 HAL 绑定配置 — 关联物理轴、IO、PLC 地址</summary>
    public class HalBinding
    {
        public Dictionary<string, string> Axes { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, int> InputIOs { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> OutputIOs { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> PlcDRegisters { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PlcMBits { get; set; } = new Dictionary<string, string>();
        public string CameraId { get; set; }
        public string ScannerId { get; set; }
    }
}
