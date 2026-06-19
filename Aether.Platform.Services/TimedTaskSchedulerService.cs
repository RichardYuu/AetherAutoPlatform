using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Services
{
    public class TimedTaskSchedulerService : ITimedTaskScheduler, IDisposable
    {
        private readonly ConcurrentDictionary<string, TimedTaskEntry> _tasks = new ConcurrentDictionary<string, TimedTaskEntry>();
        private bool _disposed;

        public void Register(string taskName, TimeSpan interval, Func<CancellationToken, Task> action)
        {
            if (_disposed) return;

            var entry = new TimedTaskEntry
            {
                TaskName = taskName,
                Interval = interval,
                Action = action,
                Cts = new CancellationTokenSource(),
                Status = new TimedTaskStatus { TaskName = taskName, NextRunTime = DateTime.Now + interval }
            };

            if (_tasks.TryAdd(taskName, entry))
            {
                RunTaskLoop(entry);
            }
        }

        public void Unregister(string taskName)
        {
            if (_tasks.TryRemove(taskName, out var entry))
            {
                entry.Cts.Cancel();
                entry.Cts.Dispose();
            }
        }

        public IReadOnlyList<TimedTaskStatus> GetStatus()
        {
            var list = new List<TimedTaskStatus>();
            foreach (var kv in _tasks)
                list.Add(kv.Value.Status);
            return list.AsReadOnly();
        }

        private async void RunTaskLoop(TimedTaskEntry entry)
        {
            while (!entry.Cts.Token.IsCancellationRequested && _tasks.ContainsKey(entry.TaskName))
            {
                var delay = entry.Status.NextRunTime.HasValue
                    ? entry.Status.NextRunTime.Value - DateTime.Now
                    : entry.Interval;

                if (delay.TotalMilliseconds > 0)
                {
                    try { await Task.Delay(delay, entry.Cts.Token); }
                    catch (TaskCanceledException) { return; }
                }

                entry.Status.IsRunning = true;
                try
                {
                    await entry.Action(entry.Cts.Token);
                    entry.Status.LastError = null;
                }
                catch (Exception ex)
                {
                    entry.Status.LastError = ex.Message;
                }
                finally
                {
                    entry.Status.IsRunning = false;
                    entry.Status.LastRunTime = DateTime.Now;
                    entry.Status.NextRunTime = DateTime.Now + entry.Interval;
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (var kv in _tasks)
            {
                kv.Value.Cts.Cancel();
                kv.Value.Cts.Dispose();
            }
            _tasks.Clear();
        }

        private class TimedTaskEntry
        {
            public string TaskName { get; set; }
            public TimeSpan Interval { get; set; }
            public Func<CancellationToken, Task> Action { get; set; }
            public CancellationTokenSource Cts { get; set; }
            public TimedTaskStatus Status { get; set; }
        }
    }
}
