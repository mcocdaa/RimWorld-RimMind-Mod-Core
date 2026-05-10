using System;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;

namespace RimMind.Kernel.Pipeline.Bus
{
    public sealed class BusPublishContext<T> : PipelineContextBase where T : AgentBusEvent
    {
        public T Event { get; set; } = null!;
        public bool IsBackground { get; set; }
        public IReadOnlyList<Delegate> Subscribers { get; set; } = Array.Empty<Delegate>();
        public List<Exception> HandlerErrors { get; } = new List<Exception>();
    }
}
