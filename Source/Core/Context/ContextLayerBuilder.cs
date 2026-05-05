using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Core.Client;
using RimMind.Core.Npc;
using RimMind.Core.Prompt;
using RimMind.Core.Settings;
using RimWorld;
using Verse;

namespace RimMind.Core.Context
{
    internal class ContextLayerBuilder
    {
        public ChatMessage? BuildL0(string npcId, string scenario, List<KeyMeta> keys, Pawn? pawn, ContextCacheManager cacheManager)
        {
            string cacheKey = $"{npcId}_{scenario}";
            if (cacheManager.L0Cache.TryGetValue(cacheKey, out var cached))
            {
                foreach (var key in keys)
                    cacheManager.PendingCacheEvents[$"L0_{key.Key}"] = true;
                return cached;
            }

            foreach (var key in keys)
                cacheManager.PendingCacheEvents[$"L0_{key.Key}"] = false;

            var sb = new StringBuilder();

            foreach (var key in keys)
            {
                var entries = pawn != null ? key.ValueProvider(pawn) : null;
                if (entries == null) continue;
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine(entry.Content);
                }
            }

            string content = sb.ToString().TrimEnd();
            if (string.IsNullOrEmpty(content)) return null;

            var msg = new ChatMessage { Role = "system", Content = content };
            cacheManager.L0Cache[cacheKey] = msg;
            return msg;
        }

        public ChatMessage? BuildL1(string npcId, List<KeyMeta> keys, Pawn? pawn, ContextCacheManager cacheManager, ContextDiffTracker diffTracker)
        {
            if (keys.Count == 0) return null;

            bool changed = false;
            if (cacheManager.L1KeyVersions.TryGetValue(npcId, out var versions))
            {
                foreach (var key in keys)
                {
                    if (pawn == null) continue;
                    string currentVal = EntriesToString(key.ValueProvider(pawn));
                    if (!versions.TryGetValue(key.Key, out var ver))
                    {
                        changed = true;
                        break;
                    }
                    if (diffTracker.KeyLastValues.TryGetValue(npcId, out var lastVals) &&
                        lastVals.TryGetValue(key.Key, out var lastVal) &&
                        lastVal != currentVal)
                    {
                        changed = true;
                        diffTracker.AddDiff(npcId, key.Key, lastVal, currentVal, key.Layer);
                    }
                }
            }
            else
            {
                changed = true;
            }

            if (!changed && cacheManager.L1BlockCache.TryGetValue(npcId, out var existingBlocks))
            {
                foreach (var key in keys)
                    cacheManager.PendingCacheEvents[$"L1_{key.Key}"] = true;
                return AssembleL1Message(existingBlocks);
            }

            foreach (var key in keys)
                cacheManager.PendingCacheEvents[$"L1_{key.Key}"] = false;

            if (!cacheManager.L1BlockCache.TryGetValue(npcId, out var blocks))
            {
                blocks = new Dictionary<string, string>();
                cacheManager.L1BlockCache[npcId] = blocks;
            }

            foreach (var key in keys)
            {
                if (pawn == null) continue;
                var entries = key.ValueProvider(pawn);
                string val = EntriesToString(entries);
                if (!string.IsNullOrEmpty(val))
                    blocks[key.Key] = val;
                else
                    blocks.Remove(key.Key);
            }

            int newVersion = (cacheManager.L1Version.TryGetValue(npcId, out var oldVer) ? oldVer : 0) + 1;
            cacheManager.L1Version[npcId] = newVersion;

            if (!cacheManager.L1KeyVersions.TryGetValue(npcId, out var keyVersions))
            {
                keyVersions = new Dictionary<string, int>();
                cacheManager.L1KeyVersions[npcId] = keyVersions;
            }
            foreach (var key in keys)
                keyVersions[key.Key] = newVersion;

            return AssembleL1Message(blocks);
        }

        public ChatMessage? BuildContextLayer(List<KeyMeta> keys, Pawn? pawn)
        {
            if (keys.Count == 0 || pawn == null) return null;

            var sb = new StringBuilder();
            foreach (var key in keys)
            {
                var entries = key.ValueProvider(pawn);
                if (entries == null) continue;
                var sbEntries = new StringBuilder();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sbEntries.AppendLine(entry.Content);
                }
                string val = sbEntries.ToString().TrimEnd();
                if (!string.IsNullOrEmpty(val))
                {
                    sb.AppendLine($"[{key.Key}]");
                    sb.AppendLine(val);
                }
            }

            string content = sb.ToString().TrimEnd();
            if (string.IsNullOrEmpty(content)) return null;

            return new ChatMessage { Role = "system", Content = content };
        }

        public ChatMessage? BuildL5(List<KeyMeta> keys, Pawn? pawn)
        {
            if (keys.Count == 0 || pawn == null) return null;
            var sb = new StringBuilder();
            sb.AppendLine("Sensor:");
            foreach (var key in keys)
            {
                var entries = key.ValueProvider(pawn);
                if (entries == null) continue;
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.Content))
                        sb.AppendLine($"  {entry.Content}");
                }
            }
            string content = sb.ToString().TrimEnd();
            if (string.IsNullOrEmpty(content)) return null;
            return new ChatMessage { Role = "system", Content = content };
        }

        public ChatMessage? BuildDiffMessage(string npcId, ContextLayer layer, ContextSnapshot snapshot, ContextDiffTracker diffTracker)
        {
            if (!diffTracker.DiffStore.TryGetValue(npcId, out var diffs) || diffs.Count == 0)
                return null;

            var layerDiffs = diffs.Where(d => d.Layer == layer).ToList();
            if (layerDiffs.Count == 0) return null;

            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Core.Prompt.EnvironmentChange".Translate());
            foreach (var diff in layerDiffs)
            {
                if (diff.Key == "map_structure")
                    sb.AppendLine(FormatMapStructureDiff(diff, snapshot));
                else
                    sb.AppendLine(diff.Format());
            }

            return new ChatMessage { Role = "system", Content = sb.ToString().TrimEnd() };
        }

        private static ChatMessage? AssembleL1Message(Dictionary<string, string> blocks)
        {
            if (blocks.Count == 0) return null;
            var sb = new StringBuilder();
            foreach (var kvp in blocks)
            {
                sb.AppendLine($"[{kvp.Key}]");
                sb.AppendLine(kvp.Value);
            }
            string content = sb.ToString().TrimEnd();
            return string.IsNullOrEmpty(content) ? null : new ChatMessage { Role = "system", Content = content };
        }

        private static string FormatMapStructureDiff(ContextDiff diff, ContextSnapshot snapshot)
        {
            var timeEntry = snapshot.AllEntries.FirstOrDefault(e =>
                e.Metadata != null && e.Metadata.TryGetValue("key", out var k) && k == "time");
            var countEntry = snapshot.AllEntries.FirstOrDefault(e =>
                e.Metadata != null && e.Metadata.TryGetValue("key", out var k) && k == "colonistCount");

            if (timeEntry != null || countEntry != null)
            {
                int hour = ContextEntryQuery.ExtractHour(snapshot.AllEntries);
                int count = ContextEntryQuery.ExtractColonistCount(snapshot.AllEntries);
                string timeStr = $"{hour:D2}:00";
                string countStr = count.ToString();
                return "RimMind.Core.Prompt.MapSummary".Translate(timeStr, countStr);
            }
            return diff.Format();
        }

        private static string EntriesToString(List<ContextEntry>? entries)
        {
            if (entries == null || entries.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Content))
                    sb.AppendLine(entry.Content);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
