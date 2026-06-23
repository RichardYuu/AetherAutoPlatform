namespace Aether.Platform.Core.Interfaces
{
    public interface IModuleView
    {
        string ModuleName { get; }
        void OnActivated();
        void OnDeactivated();
        void RefreshData();
    }
}