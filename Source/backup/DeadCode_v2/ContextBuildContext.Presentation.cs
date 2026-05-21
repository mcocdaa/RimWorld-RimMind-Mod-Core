using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Presentation.Pipeline.Context
{
    public sealed class ContextBuildContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
        public bool CacheHit { get; set; }
    }
}
