using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
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

        public ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager)
        {
            if (keys == null || keys.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var key in keys)
            {
                var entries = key.ValueProvider != null ? key.ValueProvider(pawn!) : new List<ContextEntry>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine($"[{key.Key}] {entry.Content}");
                }
            }
            if (sb.Length == 0) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = "L0" };
        }

        public ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, object? pawn, IContextCacheManager cacheManager, IContextDiffTracker diffTracker)
        {
            if (keys == null || keys.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var key in keys)
            {
                var entries = key.ValueProvider != null ? key.ValueProvider(pawn!) : new List<ContextEntry>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine($"[{key.Key}] {entry.Content}");
                }
            }
            if (sb.Length == 0) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = "L1" };
        }

        public ChatMessage? BuildContextLayer(List<KeyMeta> keys, object? pawn)
        {
            if (keys == null || keys.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var key in keys)
            {
                var entries = key.ValueProvider != null ? key.ValueProvider(pawn!) : new List<ContextEntry>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine($"[{key.Key}] {entry.Content}");
                }
            }
            if (sb.Length == 0) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = "L2" };
        }

        public ChatMessage? BuildL5(List<KeyMeta> keys, object? pawn)
        {
            if (keys == null || keys.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var key in keys)
            {
                var entries = key.ValueProvider != null ? key.ValueProvider(pawn!) : new List<ContextEntry>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine($"[{key.Key}] {entry.Content}");
                }
            }
            if (sb.Length == 0) return null;
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = "L5" };
        }

        public ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, IContextDiffTracker diffTracker)
        {
            if (!diffTracker.TryGetDiffStore(npcId, out var diffs) || diffs.Count == 0) return null;
            var filtered = diffs.FindAll(d => d.Layer == layer);
            if (filtered.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var diff in filtered)
            {
                sb.AppendLine($"[{diff.Key}] {diff.OldValue} -> {diff.NewValue}");
            }
            return new ChatMessage { Role = "system", Content = sb.ToString(), LayerTag = $"Diff-{layer}" };
        }
    }
}
