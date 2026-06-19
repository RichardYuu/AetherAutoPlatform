namespace Aether.Platform.HAL.Sim
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SimulatedCamera : IVisionSystem
    {
        public string VisionId { get; set; }
        public string CameraModel => "Simulated-Camera";
        public bool IsConnected => true;
        public string OwnerUserId { get; set; }
        public Task<bool> ConnectAsync(CancellationToken ct) => Task.FromResult(true);
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<byte[]> CaptureAsync(CancellationToken ct) => Task.FromResult(new byte[0]);
        public async Task<VisionResult> LocateAsync(string recipe, byte[] image, CancellationToken ct)
            => await Task.FromResult(new VisionResult { Success = true, Score = 0.95, X = 100, Y = 100, Angle = 0 });
        public async Task<VisionResult> MatchAsync(string recipe, byte[] image, CancellationToken ct)
            => await Task.FromResult(new VisionResult { Success = true, Score = 0.95 });
        public Task<string> ReadBarcodeAsync(byte[] image, CancellationToken ct) => Task.FromResult("SIM" + new Random().Next(100000, 999999));
        public CameraInfo GetCameraInfo() => new CameraInfo { Model = "Sim", Width = 640, Height = 480, FrameRate = 30, SerialNumber = "SIM-0001" };
    }
}
