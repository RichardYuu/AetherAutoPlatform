using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Aether.Platform.UI.Resources
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [DebuggerNonUserCode()]
    [CompilerGenerated()]
    internal class Strings
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() { }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(resourceMan, null))
                    resourceMan = new ResourceManager("Aether.Platform.UI.Resources.Strings", typeof(Strings).Assembly);
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        internal static string Common_OK { get { return ResourceManager.GetString("Common_OK", resourceCulture); } }
        internal static string Common_Cancel { get { return ResourceManager.GetString("Common_Cancel", resourceCulture); } }
        internal static string Main_Title { get { return ResourceManager.GetString("Main_Title", resourceCulture); } }
    }
}