using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Context
{
    /// <summary>
    /// Context building — snapshot construction and scheduler/embedding store access.
    /// </summary>
    public interface IContextBuilder
    {
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        ContextSnapshot? BuildSnapshot(ContextRequest request);

        IBudgetScheduler? GetScheduler();

        EmbeddingSnapshotStore? GetEmbeddingSnapshotStore();
    }
}
