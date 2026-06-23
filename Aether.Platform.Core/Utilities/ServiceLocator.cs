using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Platform.Core.Utilities
{
    /// <summary>
    /// 全局服务定位器 —— 基于 Microsoft.Extensions.DependencyInjection 的轻量封装。
    /// 提供与旧版相同的静态 API，内部委托给 IServiceProvider 实现依赖解析。
    /// </summary>
    /// <remarks>
    /// 使用方式：
    ///   1. AppBootstrap 启动时通过 SetProvider(IServiceProvider) 注入容器。
    ///   2. 任意位置通过 GetService&lt;T&gt;() / TryGetService&lt;T&gt;() 获取服务。
    ///   3. 运行时可通过 RegisterSingleton&lt;T&gt;() 动态追加注册（延迟初始化场景）。
    /// </remarks>
    public static class ServiceLocator
    {
        /// <summary>MS DI 容器，由 AppBootstrap 在初始化完成后设置</summary>
        private static IServiceProvider _provider;

        /// <summary>供运行时动态追加注册使用的内部 ServiceCollection；初始构建后保留以便惰性注册</summary>
        private static ServiceCollection _collection;

        private static readonly object _lock = new object();

        /// <summary>获取底层 IServiceProvider 实例（高级用途）</summary>
        public static IServiceProvider Provider
        {
            get
            {
                if (_provider == null)
                    throw new InvalidOperationException("ServiceProvider 尚未初始化，请先调用 AppBootstrap.Initialize()。");
                return _provider;
            }
        }

        /// <summary>
        /// 由 AppBootstrap 在服务全部注册完成后调用。
        /// 传入已 Build 的 ServiceProvider，同时保留 ServiceCollection 引用以支持后续动态注册。
        /// </summary>
        /// <param name="provider">已构建的 IServiceProvider</param>
        /// <param name="collection">用于动态追加注册的 ServiceCollection（可选，传入后即可继续使用 RegisterSingleton）</param>
        public static void SetProvider(IServiceProvider provider, ServiceCollection collection = null)
        {
            lock (_lock)
            {
                _provider = provider;
                _collection = collection;
            }
        }

        /// <summary>
        /// 运行时动态注册单例服务（用于延迟初始化场景，如 IFMS 连接建立后才注册 IIfmsBroker）。
        /// 如果已有 ServiceCollection，将追加到集合并重建小范围 Provider。
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="instance">已创建的服务实例</param>
        public static void RegisterSingleton<T>(T instance)
        {
            lock (_lock)
            {
                if (_collection != null)
                {
                    _collection.AddSingleton(typeof(T), instance);
                    _provider = _collection.BuildServiceProvider();
                }
                else
                {
                    throw new InvalidOperationException(
                        "ServiceCollection 未保留，无法动态注册。" +
                        "请确保 AppBootstrap 调用 SetProvider 时传入了 ServiceCollection。");
                }
            }
        }

        /// <summary>
        /// 运行时动态注册延迟实例化的服务。
        /// 延迟加载：直到首次 GetService&lt;T&gt;() 时才调用 factory 创建实例。
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="factory">创建服务实例的工厂方法</param>
        public static void RegisterLazy<T>(Func<object> factory)
        {
            lock (_lock)
            {
                if (_collection != null)
                {
                    _collection.AddSingleton(typeof(T), sp => factory());
                    _provider = _collection.BuildServiceProvider();
                }
                else
                {
                    throw new InvalidOperationException(
                        "ServiceCollection 未保留，无法动态注册。" +
                        "请确保 AppBootstrap 调用 SetProvider 时传入了 ServiceCollection。");
                }
            }
        }

        /// <summary>
        /// 获取已注册的服务实例。未注册时抛出 InvalidOperationException。
        /// </summary>
        /// <typeparam name="T">服务类型（接口或具体类）</typeparam>
        /// <returns>服务实例</returns>
        public static T GetService<T>()
        {
            if (_provider == null)
                throw new InvalidOperationException("ServiceProvider 尚未初始化，请先调用 AppBootstrap.Initialize()。");
            return _provider.GetRequiredService<T>();
        }

        /// <summary>
        /// 尝试获取服务实例。未注册时返回 false 并将 service 设为 default。
        /// 与 GetService 不同，此方法不抛出异常。
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">输出参数，接收服务实例或 default</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGetService<T>(out T service)
        {
            if (_provider == null)
            {
                service = default;
                return false;
            }
            service = _provider.GetService<T>();
            return service != null;
        }

        /// <summary>
        /// 重置整个容器（仅用于单元测试或应用程序重启场景）。
        /// 调用后所有已注册服务将丢失。
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _provider = null;
                _collection = null;
            }
        }
    }
}