using System;
using System.Collections.Generic;

namespace RimMind.Contracts.Pipeline
{
    public abstract class PipelineContextBase : IPipelineContext
    {
        public string TraceId { get; } = Guid.NewGuid().ToString("N").Substring(0, 12);
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;
        public bool IsShortCircuited { get; private set; }
        public string? ShortCircuitReason { get; private set; }
        public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

        public void ShortCircuit(string reason)
        {
            IsShortCircuited = true;
            ShortCircuitReason = reason;
        }
    }
}
