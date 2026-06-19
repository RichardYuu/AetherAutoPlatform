using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aether.Platform.Scripting
{
    /// <summary>
    /// 全局变量注册表 — C# 和 Lua 间共享变量的桥梁
    /// 线程安全的字符串键值对存储
    /// </summary>
    public class GlobalsRegistry
    {
        private readonly ConcurrentDictionary<string, object> _dict = new ConcurrentDictionary<string, object>();

        public void Set(string key, object value) => _dict[key] = value;
        public object Get(string key) => _dict.TryGetValue(key, out var v) ? v : null;
        public T Get<T>(string key) => _dict.TryGetValue(key, out var v) && v is T t ? t : default;
        public bool TryGet(string key, out object value) => _dict.TryGetValue(key, out value);
        public bool Remove(string key) => _dict.TryRemove(key, out _);
        public bool Contains(string key) => _dict.ContainsKey(key);
        public IReadOnlyDictionary<string, object> GetAll() => _dict;
        public void Clear() => _dict.Clear();
        public int Count => _dict.Count;
    }
}
