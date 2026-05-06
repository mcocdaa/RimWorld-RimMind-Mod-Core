using System.Collections.Generic;
using RimMind.Core.Client;

namespace RimMind.Core.Context
{
    public interface IContextLayerBuilder
    {
        ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, Verse.Pawn? pawn, IContextCacheManager cacheManager);
        ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, Verse.Pawn? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker);
        ChatMessage? BuildContextLayer(List<KeyMeta> keys, Verse.Pawn? pawn);
        ChatMessage? BuildL5(List<KeyMeta> keys, Verse.Pawn? pawn);
        ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker);
    }
}
