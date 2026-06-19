using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aether.Platform.Data.Ifms
{
    public class IfmsUploadQueue
    {
        private readonly ConcurrentQueue<IfmsUploadEntry> _queue = new ConcurrentQueue<IfmsUploadEntry>();
        private readonly object _lock = new object();

        public int Count => _queue.Count;

        public void Enqueue(string endpoint, object data, int priority = 0)
        {
            _queue.Enqueue(new IfmsUploadEntry
            {
                Endpoint = endpoint,
                Data = data,
                Priority = priority,
                EnqueueTime = DateTime.Now
            });
        }

        public bool TryDequeue(out IfmsUploadEntry entry) => _queue.TryDequeue(out entry);

        public class IfmsUploadEntry
        {
            public string Endpoint { get; set; }
            public object Data { get; set; }
            public int Priority { get; set; }
            public DateTime EnqueueTime { get; set; }
        }
    }
}
