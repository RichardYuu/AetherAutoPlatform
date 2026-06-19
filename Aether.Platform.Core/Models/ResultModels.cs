using System.Collections.Generic;

namespace Aether.Platform.Core.Models
{
    public class SpotResult
    {
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public double Diameter { get; set; }
        public double Ellipticity { get; set; }
    }

    public class VisionResult
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        public string Barcode { get; set; }
        public Dictionary<string, object> ExtraData { get; set; }
    }

    public class CameraInfo
    {
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
    }

    public class StatusChangedEventArgs
    {
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
    }
}