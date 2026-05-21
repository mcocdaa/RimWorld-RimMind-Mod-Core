using System;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Pipeline.AI
{
    public sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; set; } = null!;
        public Result<AIResponse, RimMindError>? Result { get; set; }
        public int RetryCount { get; set; }
        public IAIClient? Client { get; set; }
        public TimeSpan Elapsed { get; set; }
        public ContextSnapshot? Snapshot { get; set; }

        public AIRequestContext() : base() { }

        public AIRequestContext(IAIClient client) : base()
        {
            Client = client;
        }

        public void Reset(IAIClient client)
        {
            Request = null!;
            Result = null;
            RetryCount = 0;
            Client = client;
            Elapsed = TimeSpan.Zero;
            Snapshot = null;
        }

        public void Clear()
        {
            Request = null!;
            Result = null;
            Client = null;
            Snapshot = null;
        }
    }
}
