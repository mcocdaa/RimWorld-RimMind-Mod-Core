using System;
using System.Collections.Generic;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.Bus
{
    public sealed class BusPublishContext<T> : PipelineContextBase where T : AgentBusEvent
    {
        public T Event { get; set; } = null!;
        public bool IsBackground { get; set; }
        public IReadOnlyList<Delegate> Subscribers { get; set; } = Array.Empty<Delegate>();
        public List<RimMindError> HandlerErrors { get; } = new List<RimMindError>();
    }
}
