using System;
using RimMind.Contracts.Client;
using RimMind.Contracts.Pipeline;

namespace RimMind.Core.Pipeline.AI
{
    public sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; set; } = null!;
        public AIResponse? Response { get; set; }
        public Exception? Error { get; set; }
        public int RetryCount { get; set; }
        public IAIClient? Client { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
