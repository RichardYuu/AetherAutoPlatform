namespace Aether.Platform.HAL.Sim
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.HAL;
    using Core.Models;

    public class SimulatedHardwareService : IHardwareService
    {
        private readonly Dictionary<string, IAxis> _axes = new Dictionary<string, IAxis>();
        private readonly Dictionary<string, IDigitalIO> _ios = new Dictionary<string, IDigitalIO>();
        private readonly Dictionary<string, IBarcodeScanner> _scanners = new Dictionary<string, IBarcodeScanner>();
        private readonly Dictionary<string, IVisionSystem> _visions = new Dictionary<string, IVisionSystem>();
        private readonly Dictionary<int, IAnalogIO> _analogIOs = new Dictionary<int, IAnalogIO>();
        private readonly Dictionary<string, IScaleDevice> _scales = new Dictionary<string, IScaleDevice>();
        private readonly Dictionary<string, ITemperatureController> _temperatures = new Dictionary<string, ITemperatureController>();
        private readonly Dictionary<string, IElectricProportionalValve> _epValves = new Dictionary<string, IElectricProportionalValve>();
        private SimulatedPLC _plc;

        public IAxis GetAxis(string axisId)
        {
            if (!_axes.TryGetValue(axisId, out var axis))
            { axis = new SimulatedAxis { AxisId = axisId }; _axes[axisId] = axis; }
            return axis;
        }

        public IDigitalIO GetDigitalIO(string ioId)
        {
            if (!_ios.TryGetValue(ioId, out var io))
            { io = new SimulatedDigitalIO { IOId = ioId }; _ios[ioId] = io; }
            return io;
        }

        public IBarcodeScanner GetScanner(string scannerId)
        {
            if (!_scanners.TryGetValue(scannerId, out var scanner))
            { scanner = new SimulatedScanner { ScannerId = scannerId }; _scanners[scannerId] = scanner; }
            return scanner;
        }

        public IVisionSystem GetVisionSystem(string visionId)
        {
            if (!_visions.TryGetValue(visionId, out var vision))
            { vision = new SimulatedCamera { VisionId = visionId }; _visions[visionId] = vision; }
            return vision;
        }

        public IPlcCommunication GetPlc() => _plc ?? (_plc = new SimulatedPLC());
        public ISerialPort GetSerialPort(string portName) => new SimulatedSerialPort { PortName = portName };
        public IModbusCommunication GetModbus(ModbusType type, string conn) => new SimulatedModbus { Type = type };
        public IAnalogIO GetAnalogIO(int channel)
        {
            if (!_analogIOs.TryGetValue(channel, out var aio))
            { aio = new SimulatedAnalogIO { Channel = channel }; _analogIOs[channel] = aio; }
            return aio;
        }

        public IScaleDevice GetScale(string scaleId)
        {
            if (!_scales.TryGetValue(scaleId, out var scale))
            { scale = new SimulatedScale { ScaleId = scaleId }; _scales[scaleId] = scale; }
            return scale;
        }

        public IHeightGauge GetHeightGauge(string gaugeId) => new SimulatedHeightGauge { GaugeId = gaugeId };
        public ITemperatureController GetTemperature(string tempId)
        {
            if (!_temperatures.TryGetValue(tempId, out var temp))
            { temp = new SimulatedTemperature { ControllerId = tempId }; _temperatures[tempId] = temp; }
            return temp;
        }

        public IMicropressureSensor GetMicropressure(string sensorId) => new SimulatedMicropressure { SensorId = sensorId };
        public IElectricProportionalValve GetEPValve(string valveId)
        {
            if (!_epValves.TryGetValue(valveId, out var valve))
            { valve = new SimulatedEPValve { ValveId = valveId }; _epValves[valveId] = valve; }
            return valve;
        }

        public IExposureMeter GetExposureMeter(string meterId) => new SimulatedExposureMeter { MeterId = meterId };
        public ISpotAnalyzer GetSpotAnalyzer(string analyzerId) => new SimulatedSpotAnalyzer { AnalyzerId = analyzerId };
        public bool HasPlc => true;
    }
}
