using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class ContextLayerBuilder : IContextLayerBuilder
    {
        private readonly IContextKeyProvider _keyProvider;
        private readonly ILogSink? _log;

        public ContextLayerBuilder(IContextKeyProvider keyProvider, ILogSink? log = null)
        {
            _keyProvider = keyProvider;
            _log = log;
        }

        public async Task<List<ContextEntry>> BuildLayerAsync(
            List<KeyMeta> keys, object? pawn, ProviderContext ctx,
            ProviderCache? cache, CancellationToken ct)
        {
            if (keys == null || keys.Count == 0) return new List<ContextEntry>();
            var entries = new List<ContextEntry>();
            foreach (var key in keys)
            {
                ct.ThrowIfCancellationRequested();
                if (key.Def is ContextProviderDef def)
                {
                    string? value = cache != null
                        ? await cache.GetOrComputeAsync(def, ctx, ct).ConfigureAwait(false)
                        : await def.Provider(ctx, ct).ConfigureAwait(false);
                    if (value != null)
                        entries.Add(new ContextEntry { SourceKey = key.Key, Content = value });
                }
                else if (key.ValueProvider != null)
                {
                    var result = key.ValueProvider(pawn!);
                    entries.AddRange(result);
                }
            }
            return entries;
        }

        public ChatMessage? EntriesToLayerMessage(List<ContextEntry> entries, string layerTag)
        {
            if (entries == null || entries.Count == 0) return null;
            var sb = new StringBuilder();
            sb.AppendLine($"<layer_{layerTag}>");
            bool hasContent = false;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Content))
                {
                    sb.AppendLine($"[{entry.SourceKey}] {entry.Content}");
                    hasContent = true;
                }
            }
            sb.AppendLine($"</layer_{layerTag}>");
            if (!hasContent) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = layerTag };
        }

        public ChatMessage? BuildLayer(List<KeyMeta> keys, string xmlTag, string layerTag, object? pawn)
        {
            if (keys == null || keys.Count == 0) return null;
            var sb = new StringBuilder();
            sb.AppendLine($"<layer_{xmlTag}>");
            bool hasContent = false;
            foreach (var key in keys)
            {
                var entries = key.ValueProvider != null ? key.ValueProvider(pawn!) : new List<ContextEntry>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                    {
                        sb.AppendLine($"[{key.Key}] {entry.Content}");
                        hasContent = true;
                    }
                }
            }
            sb.AppendLine($"</layer_{xmlTag}>");
            if (!hasContent) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = layerTag };
        }

        public ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager)
            => BuildLayer(keys, "L0_Static", "L0", pawn);

        public ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker)
            => BuildLayer(keys, "L1", "L1", pawn);

        public ChatMessage? BuildContextLayer(List<KeyMeta> keys, object? pawn)
            => BuildLayer(keys, "L2", "L2", pawn);

        public ChatMessage? BuildL3(List<KeyMeta> keys, object? pawn)
            => BuildLayer(keys, "L3", "L3", pawn);

        public ChatMessage? BuildL5(List<KeyMeta> keys, object? pawn)
            => BuildLayer(keys, "L5", "L5", pawn);

        public ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker)
        {
            if (!diffTracker.TryGetDiffStore(npcId, out var diffs) || diffs.Count == 0) return null;
            var filtered = diffs.FindAll(d => d.Layer == layer);
            if (filtered.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<layer_diff_{layer}>");
            foreach (var diff in filtered)
            {
                sb.AppendLine($"[{diff.Key}] {diff.OldValue} -> {diff.NewValue}");
            }
            sb.AppendLine($"</layer_diff_{layer}>");
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = $"Diff-{layer}" };
        }
    }
}
