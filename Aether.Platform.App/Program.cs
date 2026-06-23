using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Aether.Platform.App
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var bootstrap = new AppBootstrap
            {
                Mode = RuntimeMode.Simulation,
            };

            bootstrap.OnSystemLog += (cat, msg) =>
            {
                System.Diagnostics.Debug.WriteLine($"[AppBootstrap:{cat}] {msg}");
            };

            bootstrap.Initialize();

            bootstrap.LoadDevice("MTFTest");

            var uiAssemblyPath = ResolveUIAssemblyPath();
            var assembly = Assembly.LoadFrom(uiAssemblyPath);
            var shellType = assembly.GetType("Aether.Platform.UI.Shell.MainShellForm");
            Application.Run((Form)Activator.CreateInstance(shellType));
        }

        private static string ResolveUIAssemblyPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var localPath = Path.Combine(baseDir, "Aether.Platform.UI.dll");
            if (File.Exists(localPath))
                return localPath;

#if DEBUG
            var config = "Debug";
#else
            var config = "Release";
#endif
            var devPath = Path.GetFullPath(Path.Combine(
                baseDir, "..", "Aether.Platform.UI", "bin", config,
                "Aether.Platform.UI.dll"));
            if (File.Exists(devPath))
                return devPath;

            return localPath;
        }
    }
}