using System;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Models.Pipeline
{
    public class PerceptionBufferEntry
    {
        public string Source { get; set; } = "";
        public string Content { get; set; } = "";
        public float Priority { get; set; }
        public long TimestampTicks { get; set; }
        public string? Layer { get; set; }
        public string? NpcId { get; set; }
    }
}
