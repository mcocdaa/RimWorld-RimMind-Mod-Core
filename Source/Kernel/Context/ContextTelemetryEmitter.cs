using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Kernel.Context;
using RimMind.Kernel.Logging;

namespace RimMind.Kernel.Context
{
    public class ContextTelemetryEmitter
    {
        private readonly EmbeddingSnapshotStore _embeddingSnapshotStore;

        public ContextTelemetryEmitter(EmbeddingSnapshotStore embeddingSnapshotStore)
        {
            _embeddingSnapshotStore = embeddingSnapshotStore;
        }

        public void EmitForSnapshot(
            ContextSnapshot snapshot,
            List<KeyMeta> filteredKeys,
            BudgetAllocation schedule,
            object? pawn,
            IContextDiffTracker diffTracker,
            IContextCacheManager cacheManager)
        {
            if (schedule == null) return;

            try
            {
                var allSnapshotKeys = schedule.L0Keys.Concat(schedule.L1Keys)
                    .Concat(schedule.L2Keys).Concat(schedule.L3Keys).Concat(schedule.L5Keys);
                foreach (var key in allSnapshotKeys)
                {
                    string sourceText = "";
                    if (diffTracker.TryGetKeyLastValues(snapshot.NpcId, out var vals) &&
                        vals.TryGetValue(key.Key, out var val))
                    {
                        sourceText = val.Length > 500
                            ? (char.IsHighSurrogate(val[499]) ? val.Substring(0, 499) : val.Substring(0, 500))
                            : val;
                    }
                    float[]? vector = SemanticEmbedding.GetBlockEmbedding(snapshot.NpcId, key.Key);
                    if (vector == null && key.KeyEmbedding != null)
                        vector = key.KeyEmbedding;
                    float relevanceScore = snapshot.KeyScores.TryGetValue(key.Key, out var score) ? score : 0f;
                    _embeddingSnapshotStore.Record(new EmbeddingSnapshotRecord
                    {
                        NpcId = snapshot.NpcId,
                        ScenarioId = snapshot.Scenario,
                        Key = key.Key,
                        Layer = key.Layer.ToString(),
                        SourceText = sourceText,
                        Vector = vector!,
                        RelevanceScore = relevanceScore,
                        TimestampTicks = DateTime.Now.Ticks,
                    });
                }
            }
            catch (Exception ex) { RimMindLogger.Warning($"Embedding snapshot failed: {ex.Message}"); }
        }
    }
}
