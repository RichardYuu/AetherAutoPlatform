using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Core.Interfaces
{
    /// <summary>
    /// 模块视图接口 —— 所有 UI 模块必须实现此接口，用于导航管理器的统一调度。
    /// 采用 MVP 模式：View 负责纯 UI 渲染，Presenter 负责业务逻辑。
    /// </summary>
    /// <remarks>
    /// 实现示例：
    /// <code>
    /// public class MainView : UserControl, IModuleView
    /// {
    ///     public string ModuleName =&gt; "Main";
    ///     public void OnActivated() { /* 切到页面时刷新数据 */ }
    ///     public void OnDeactivated() { /* 切离时保存状态 */ }
    ///     public void RefreshData() { /* 定时刷新 */ }
    /// }
    /// </code>
    /// </remarks>
    public interface IModuleView
    {
        /// <summary>模块标识符，用于导航管理器注册和切换</summary>
        string ModuleName { get; }

        /// <summary>
        /// 切换到该模块时调用。
        /// 用于恢复定时器、重新订阅事件、刷新数据。
        /// 由 NavigationManager 在 NavigateTo 时自动调用。
        /// </summary>
        void OnActivated();

        /// <summary>
        /// 切离该模块时调用。
        /// 用于保存编辑态、停止定时器、取消事件订阅。
        /// 由 NavigationManager 在切换前自动调用。
        /// </summary>
        void OnDeactivated();

        /// <summary>
        /// 手动或定时刷新页面数据。
        /// 由 UI 刷新定时器（100ms）或用户操作触发。
        /// </summary>
        void RefreshData();
    }
}
