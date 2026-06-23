namespace Aether.Platform.App.Properties
{
    [global::System.Configuration.SettingsProvider(typeof(global::System.Configuration.LocalFileSettingsProvider))]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        public static Settings Default { get { return defaultInstance; } }
    }
}