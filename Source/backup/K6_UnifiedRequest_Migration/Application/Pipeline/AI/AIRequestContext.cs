using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.AI
{
    public sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; set; } = null!;
        public AIResponse? Response { get; set; }
        public IAIClient? Client { get; set; }
        public ContextSnapshot? Snapshot { get; set; }
        public bool CacheHit { get; set; }
        public string? CacheKey { get; set; }
        public int RetryCount { get; set; }
        public Result<AIResponse, RimMindError>? Result { get; set; }
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Tool call results populated by ToolCallDispatchMiddleware.
        /// Null if no tool calls were dispatched.
        /// </summary>
        public List<ToolResult>? ToolCallResults { get; set; }

        public AIRequestContext() : base() { }

        public AIRequestContext(AIRequest request, string? traceId = null, System.Threading.CancellationToken ct = default)
            : base(traceId ?? request.TraceId, ct)
        {
            Request = request;
        }

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
            ToolCallResults = null;
        }

        public void Clear()
        {
            Request = null!;
            Result = null;
            Client = null;
            Snapshot = null;
            ToolCallResults = null;
        }
    }
}
