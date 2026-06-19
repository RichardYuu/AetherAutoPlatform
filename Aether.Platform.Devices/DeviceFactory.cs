using Aether.Platform.Core.Interfaces;

namespace Aether.Platform.Devices
{
    public class DeviceFactory
    {
        public static IDevice Create(string type)
        {
            switch (type)
            {
                case "MTFTest": return new MTFTest.MTFTestDevice();
                case "ZG13":    return new ZG13.ZG13Device();
                case "XL07":    return new XL07.XL07Device();
                case "PX09":    return new PX09.PX09Device();
                case "DJ10":    return new DJ10.DJ10Device();
                case "ZZ14":    return new ZZ14.ZZ14Device();
                case "BB08":    return new BB08.BB08Device();
                case "DJ16":    return new DJ16.DJ16Device();
                case "SF16":    return new SF16.SF16Device();
                case "DB06":    return new DB06.DB06Device();
                case "CC03":    return new CC03.CC03Device();
                case "AssemblyNew": return new AssemblyNew.AssemblyNewDevice();
                case "Assembly": return new Assembly.AssemblyDevice();
                default: return null;
            }
        }

        /// <summary>所有已注册的设备类型清单</summary>
        public static string[] RegisteredTypes => new[]
        {
            "MTFTest", "ZG13", "XL07", "PX09", "DJ10", "ZZ14",
            "BB08", "DJ16", "SF16", "DB06", "CC03", "AssemblyNew", "Assembly"
        };
    }
}
