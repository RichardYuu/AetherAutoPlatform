namespace Aether.Platform.HAL
{
    using Core.Interfaces;

    public static class HardwareServiceFactory
    {
        public static IHardwareService Create(HardwareServiceMode mode)
        {
            switch (mode)
            {
                case HardwareServiceMode.Real: return new Real.RealHardwareService();
                case HardwareServiceMode.Simulated: return new Sim.SimulatedHardwareService();
                default: return new Sim.SimulatedHardwareService();
            }
        }
    }
}
