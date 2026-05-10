using System.Collections.Generic;
using RimMind.Contracts.Client;

namespace RimMind.Contracts.Context
{
    public interface IContextLayerBuilder
    {
        ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager);
        ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker);
        ChatMessage? BuildContextLayer(List<KeyMeta> keys, object? pawn);
        ChatMessage? BuildL5(List<KeyMeta> keys, object? pawn);
        ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker);
    }
}
