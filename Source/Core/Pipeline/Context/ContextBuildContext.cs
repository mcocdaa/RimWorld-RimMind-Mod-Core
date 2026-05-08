using RimMind.Kernel.Context;
using RimMind.Contracts.Pipeline;

namespace RimMind.Core.Pipeline.Context
{
    public sealed class ContextBuildContext : PipelineContextBase
    {
        public ContextRequest Request { get; set; } = null!;
        public ContextSnapshot? Snapshot { get; set; }
    }
}
