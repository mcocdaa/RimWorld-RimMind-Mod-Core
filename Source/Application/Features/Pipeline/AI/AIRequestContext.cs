using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.AI
{
    internal sealed class AIRequestContext : PipelineContextBase
    {
        public AIRequest Request { get; }
        public AIResponse? Response { get; set; }
        public IAIClient? Client { get; set; }
        public ContextSnapshot? Snapshot { get; set; }
        public bool CacheHit { get; set; }
        public string? CacheKey { get; set; }
        public int RetryCount { get; set; }
        public Result<AIResponse, RimMindError>? Result { get; set; }

        public AIRequestContext(AIRequest request, string? traceId = null, System.Threading.CancellationToken ct = default)
            : base(traceId ?? request.TraceId, ct)
        {
            Request = request;
        }
    }
}
