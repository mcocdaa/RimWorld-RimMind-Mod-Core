using System;
using RimMind.Core.Client;
using RimMind.Kernel.Pipeline;

namespace RimMind.Contracts.Pipeline.AI
{
    public sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; init; } = null!;
        public AIResponse? Response { get; set; }
        public Exception? Error { get; set; }
        public int RetryCount { get; set; }
        public IAIClient? Client { get; init; }
        public TimeSpan Elapsed { get; set; }
    }
}
