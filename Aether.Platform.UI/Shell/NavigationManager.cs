using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Aether.Platform.UI.Shell
{
    public class NavigationManager
    {
        private readonly Panel _container;
        private readonly Dictionary<string, Control> _cache = new Dictionary<string, Control>();
        private readonly Dictionary<string, Func<Control>> _factories = new Dictionary<string, Func<Control>>();
        private Control _currentView;

        public string ActiveModule { get; private set; }

        public NavigationManager(Panel container)
        {
            _container = container;
        }

        public void RegisterModule(string name, Func<Control> factory)
        {
            _factories[name] = factory;
        }

        public void NavigateTo(string moduleName)
        {
            if (ActiveModule == moduleName && _currentView != null)
                return;

            if (_currentView != null)
                _currentView.Visible = false;

            if (!_cache.TryGetValue(moduleName, out var view))
            {
                if (!_factories.TryGetValue(moduleName, out var factory))
                    return;

                view = factory();
                view.Dock = DockStyle.Fill;
                _container.Controls.Add(view);
                _cache[moduleName] = view;
            }

            view.Visible = true;
            view.BringToFront();
            _currentView = view;
            ActiveModule = moduleName;

            if (view is Core.Interfaces.IModuleView moduleView)
                moduleView.OnActivated();
        }

        public T GetModule<T>(string moduleName) where T : Control
        {
            _cache.TryGetValue(moduleName, out var view);
            return view as T;
        }
    }
}