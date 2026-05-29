using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Context
{
    public interface IContextLayerBuilder
    {
        ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager);
        ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker);
        ChatMessage? BuildContextLayer(List<KeyMeta> keys, object? pawn);
        ChatMessage? BuildL3(List<KeyMeta> keys, object? pawn);
        ChatMessage? BuildL5(List<KeyMeta> keys, object? pawn);
        ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker);

        Task<List<ContextEntry>> BuildLayerAsync(List<KeyMeta> keys, object? pawn, ProviderContext ctx, ProviderCache? cache, CancellationToken ct);
        ChatMessage? EntriesToLayerMessage(List<ContextEntry> entries, string layerTag);
    }
}
