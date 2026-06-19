using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Utilities;
using Aether.Platform.Data.Configuration;
using Aether.Platform.Data.Database;

namespace Aether.Platform.Services
{
    public class InitializationService : IInitializationService
    {
        private readonly List<string> _errors = new List<string>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public bool IsInitialized { get; private set; }

        public InitializationService()
        {
            IsInitialized = false;
        }

        public async Task<bool> InitializeAsync(CancellationToken ct)
        {
            _lock.EnterWriteLock();
            try { _errors.Clear(); }
            finally { _lock.ExitWriteLock(); }

            var steps = new (string name, Func<CancellationToken, Task<bool>> action)[]
            {
                ("加载配置文件", c => { ConfigManager.Load(); return Task.FromResult(true); }),
                ("初始化数据库", c =>
                {
                    try
                    {
                        var config = ConfigManager.Load();
                        var provider = DatabaseProviderFactory.Create(
                            config.DatabaseMode, config.SqlServerConnectionString, config.AccessConnectionString);
                        return Task.FromResult(provider.IsAvailable);
                    }
                    catch (Exception ex)
                    {
                        AddError($"数据库初始化失败: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }),
                ("初始化 IFMS 连接", c =>
                {
                    try
                    {
                        var ifms = new Data.Ifms.IfmsHttpClient();
                        ServiceLocator.RegisterSingleton<IIfmsBroker>(ifms);
                        return Task.FromResult(true);
                    }
                    catch (Exception ex)
                    {
                        AddError($"IFMS 初始化失败: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }),
                ("加载参数目录", c =>
                {
                    try
                    {
                        var config = ConfigManager.Load();
                        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.ParameterDirectory);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        return Task.FromResult(true);
                    }
                    catch (Exception ex)
                    {
                        AddError($"参数目录创建失败: {ex.Message}");
                        return Task.FromResult(false);
                    }
                }),
            };

            foreach (var step in steps)
            {
                if (ct.IsCancellationRequested) return false;
                try { await step.action(ct); }
                catch (Exception ex) { AddError($"{step.name}: {ex.Message}"); }
            }

            IsInitialized = _errors.Count == 0;
            return IsInitialized;
        }

        private void AddError(string error)
        {
            _lock.EnterWriteLock();
            try { _errors.Add(error); }
            finally { _lock.ExitWriteLock(); }
        }

        public IEnumerable<string> GetErrors()
        {
            _lock.EnterReadLock();
            try { return new List<string>(_errors); }
            finally { _lock.ExitReadLock(); }
        }
    }
}
