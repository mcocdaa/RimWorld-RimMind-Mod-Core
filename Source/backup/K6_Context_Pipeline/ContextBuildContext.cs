using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    public sealed class ContextBuildContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public bool CacheHit { get; set; }

        public ContextBuildContext() : base() { }

        public ContextBuildContext(ContextRequest request, string? traceId = null, System.Threading.CancellationToken ct = default)
            : base(traceId, ct)
        {
            Request = request;
        }
    }
}
