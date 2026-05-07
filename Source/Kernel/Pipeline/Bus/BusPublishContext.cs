using System;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Kernel.Pipeline;

namespace RimMind.Contracts.Pipeline.Bus
{
    public sealed class BusPublishContext<T> : PipelineContextBase where T : AgentBusEvent
    {
        public T Event { get; init; } = null!;
        public bool IsBackground { get; init; }
        public IReadOnlyList<Delegate> Subscribers { get; init; } = Array.Empty<Delegate>();
        public List<Exception> HandlerErrors { get; } = new List<Exception>();
    }
}
