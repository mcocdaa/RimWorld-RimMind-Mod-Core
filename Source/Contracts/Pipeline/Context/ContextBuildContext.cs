using RimMind.Kernel.Context;
using RimMind.Contracts.Pipeline;

namespace RimMind.Contracts.Pipeline.Context
{
    public sealed class ContextBuildContext : PipelineContextBase
    {
        public ContextRequest Request { get; init; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
    }
}
