using System;
using RimMind.Contracts.Client;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Context;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; set; } = null!;
        public AIResponse? Response { get; set; }
        public Exception? Error { get; set; }
        public int RetryCount { get; set; }
        public IAIClient? Client { get; set; }
        public TimeSpan Elapsed { get; set; }
        public ContextSnapshot? Snapshot { get; set; }
    }
}
