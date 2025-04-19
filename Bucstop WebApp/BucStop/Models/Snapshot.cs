using System;
using System.Collections.Generic;

namespace BucStop.Models
{
    public class Snapshot
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, int> PlayerCounts { get; set; }
        public Dictionary<string, string> Logs { get; set; }
        public string Description { get; set; }

        public Snapshot()
        {
            Id = Guid.NewGuid().ToString();
            PlayerCounts = new Dictionary<string, int>();
            Logs = new Dictionary<string, string>();
        }
    }
} 