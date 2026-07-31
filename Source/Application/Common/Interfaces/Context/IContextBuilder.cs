using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        /// <summary>
        /// Build snapshot directly from envelope fields, bypassing ContextRequest.
        /// K-phase unified path: avoids dependency on legacy ContextRequest DTO.
        /// </summary>
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        ContextSnapshot? BuildSnapshotFromEnvelope(string npcId, string? currentQuery, int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null);

        /// <summary>
        /// Async version of BuildSnapshotFromEnvelope.
        /// Uses async context providers when available (KeyMeta.Def != null).
        /// Falls back to synchronous providers for legacy keys.
        /// </summary>
        Task<ContextSnapshot?> BuildSnapshotFromEnvelopeAsync(string npcId, string? currentQuery,
            int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null,
            HashSet<string>? skipLayers = null,
            CancellationToken ct = default);

        IBudgetScheduler? GetScheduler();

        EmbeddingSnapshotStore? GetEmbeddingSnapshotStore();
    }
}
