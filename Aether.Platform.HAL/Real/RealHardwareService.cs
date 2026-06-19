namespace Aether.Platform.HAL.Real
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class RealHardwareService : IHardwareService
    {
        private readonly Dictionary<string, IAxis> _axes = new Dictionary<string, IAxis>();
        private readonly Dictionary<string, IDigitalIO> _ios = new Dictionary<string, IDigitalIO>();
        private readonly Dictionary<string, IBarcodeScanner> _scanners = new Dictionary<string, IBarcodeScanner>();
        private readonly Dictionary<string, IVisionSystem> _visions = new Dictionary<string, IVisionSystem>();
        private readonly Dictionary<string, ISerialPort> _serialPorts = new Dictionary<string, ISerialPort>();
        private readonly Dictionary<int, IAnalogIO> _analogIOs = new Dictionary<int, IAnalogIO>();
        private readonly Dictionary<string, IScaleDevice> _scales = new Dictionary<string, IScaleDevice>();
        private IPlcCommunication _plc = null;

        public IAxis GetAxis(string axisId)
        {
            if (!_axes.TryGetValue(axisId, out var axis))
            {
                axis = new LeadshineMotion { AxisId = axisId };
                _axes[axisId] = axis;
            }
            return axis;
        }

        public IDigitalIO GetDigitalIO(string ioId)
        {
            if (!_ios.TryGetValue(ioId, out var io))
            {
                io = new DigitalIOController { IOId = ioId };
                _ios[ioId] = io;
            }
            return io;
        }

        public IBarcodeScanner GetScanner(string scannerId) => null;
        public IVisionSystem GetVisionSystem(string visionId) => null;
        public IPlcCommunication GetPlc() => _plc;
        public ISerialPort GetSerialPort(string portName) => null;
        public IModbusCommunication GetModbus(ModbusType type, string conn) => null;
        public IAnalogIO GetAnalogIO(int channel) => null;
        public IScaleDevice GetScale(string scaleId) => null;
        public IHeightGauge GetHeightGauge(string gaugeId) => null;
        public ITemperatureController GetTemperature(string tempId) => null;
        public IMicropressureSensor GetMicropressure(string sensorId) => null;
        public IElectricProportionalValve GetEPValve(string valveId) => null;
        public IExposureMeter GetExposureMeter(string meterId) => null;
        public ISpotAnalyzer GetSpotAnalyzer(string analyzerId) => null;
        public bool HasPlc => _plc != null;
    }
}
