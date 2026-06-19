using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Data.Configuration;

namespace Aether.Platform.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>();
        private string _currentLanguage;

        public string CurrentLanguage => _currentLanguage;
        public string[] SupportedLanguages { get; } = { "zh-CN", "en", "vi-VN" };

        public event Action<string> OnLanguageChanged;

        public LocalizationService()
        {
            var config = ConfigManager.Load();
            _currentLanguage = config.CurrentLanguage ?? "zh-CN";
            LoadDefaultResources();
        }

        public void SwitchTo(string cultureName)
        {
            if (_currentLanguage == cultureName) return;
            _currentLanguage = cultureName;

            var config = ConfigManager.Load();
            config.CurrentLanguage = cultureName;
            ConfigManager.Save();

            OnLanguageChanged?.Invoke(cultureName);
        }

        public string T(string key)
        {
            if (_resources.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_currentLanguage, out var result))
                    return result;
                if (translations.TryGetValue("zh-CN", out var fallback))
                    return fallback;
            }
            return key;
        }

        public string T(string key, params object[] args)
        {
            var template = T(key);
            return args != null && args.Length > 0 ? string.Format(template, args) : template;
        }

        public void RegisterResource(string key, string zhCN, string en, string viVN = null)
        {
            _resources[key] = new Dictionary<string, string>
            {
                ["zh-CN"] = zhCN,
                ["en"] = en,
                ["vi-VN"] = viVN ?? zhCN
            };
        }

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                if (data != null)
                {
                    foreach (var kv in data) _resources[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        private void LoadDefaultResources()
        {
            RegisterResource("app.title", "Aether自动化平台", "Aether Automation Platform");
            RegisterResource("menu.main", "主界面", "Main");
            RegisterResource("menu.status_log", "状态日志", "Status Log");
            RegisterResource("menu.control_debug", "控制调试", "Control Debug");
            RegisterResource("menu.vision_debug", "视觉调试", "Vision Debug");
            RegisterResource("menu.process_debug", "工艺调试", "Process Debug");
            RegisterResource("menu.system_config", "系统参数", "System Config");
            RegisterResource("menu.history", "历史记录", "History");
            RegisterResource("menu.version_info", "版本信息", "Version Info");
            RegisterResource("menu.login", "登录", "Login");
            RegisterResource("btn.run", "运行", "Run");
            RegisterResource("btn.pause", "暂停", "Pause");
            RegisterResource("btn.reset", "复位", "Reset");
            RegisterResource("btn.stop", "停止", "Stop");
            RegisterResource("btn.emergency_stop", "急停", "E-Stop");
            RegisterResource("btn.standby", "待机", "Standby");
            RegisterResource("btn.auto_mode", "自动模式", "Auto Mode");
            RegisterResource("status.idle", "空闲", "Idle");
            RegisterResource("status.running", "运行中", "Running");
            RegisterResource("status.paused", "已暂停", "Paused");
            RegisterResource("status.error", "故障", "Error");
            RegisterResource("alarm.info", "提示", "Info");
            RegisterResource("alarm.warning", "警告", "Warning");
            RegisterResource("alarm.error", "错误", "Error");
        }
    }
}
