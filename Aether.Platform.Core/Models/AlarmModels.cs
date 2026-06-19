using System;
using System.Collections.Generic;

namespace Aether.Platform.Core.Models
{
    public class AlarmRecord
    {
        public string Code { get; set; }
        public AlarmLevel Level { get; set; }
        public string Description { get; set; }
        public string Suggestion { get; set; }
        public DateTime OccurTime { get; set; }
        public DateTime? ClearedTime { get; set; }
        public bool IsActive { get; set; }
        public string HandledBy { get; set; }
    }

    public class FaultCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Cause { get; set; }
        public string Solution { get; set; }
        public AlarmLevel Severity { get; set; }
    }
}