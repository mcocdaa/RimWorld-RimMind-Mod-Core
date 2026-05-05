using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RimMind.Core.Client;
using RimMind.Core.Flywheel;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Prompt;
using RimMind.Core.Settings;
using RimWorld;
using Verse;

namespace RimMind.Core.Context
{
    public class ContextEngine : IContextEngine, IDisposable
    {
        private bool _disposed;
        private bool _needsFullRebuild = true;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        private readonly BudgetScheduler _scheduler = new BudgetScheduler();
        private readonly HistoryManager _historyManager;
        private readonly ContextCacheManager _cacheManager = new ContextCacheManager();
        private readonly ContextDiffTracker _diffTracker = new ContextDiffTracker();
        private readonly ContextLayerBuilder _layerBuilder = new ContextLayerBuilder();
        private readonly EmbeddingSnapshotStore _embeddingSnapshotStore = new EmbeddingSnapshotStore();

        public ContextEngine(HistoryManager historyManager)
        {
            _historyManager = historyManager;
        }

        public BudgetScheduler GetScheduler() => _scheduler;
        public EmbeddingSnapshotStore GetEmbeddingSnapshotStore() => _embeddingSnapshotStore;

        internal void TouchCache(string cacheKey)
        {
            if (_disposed) return;
            _cacheManager.TouchCache(cacheKey);
        }

        internal void RemoveL0CacheForNpc(string npcId)
        {
            if (_disposed) return;
            _cacheManager.RemoveL0CacheForNpc(npcId);
        }

        public ContextSnapshot BuildSnapshot(ContextRequest request)
        {
            if (_disposed) return null;
            _rwLock.EnterWriteLock();
            try
            {
                var snapshot = BuildSnapshotCore(request);
                if (snapshot != null)
                    CommitSnapshotSideEffects(snapshot);
                return snapshot;
            }
            finally
            {
                ContextKeyRegistry.CurrentScenario = string.Empty;
                _cacheManager.PendingCacheEvents.Clear();
                _rwLock.ExitWriteLock();
            }
        }

        private ContextSnapshot BuildSnapshotCore(ContextRequest request)
        {
            string scenario = request.Scenario ?? ScenarioIds.Dialogue;
            string l0CacheKey = $"{request.NpcId}_{scenario}";
            _cacheManager.TouchCache(l0CacheKey);
            ContextKeyRegistry.CurrentScenario = scenario;

            bool wasFullRebuild = _needsFullRebuild;
            if (_needsFullRebuild)
            {
                _diffTracker.RemoveNpcKeyLastValues(request.NpcId);
                _diffTracker.ClearNpcDiffs(request.NpcId);
                _needsFullRebuild = false;
            }

            var snapshot = new ContextSnapshot
            {
                NpcId = request.NpcId,
                Scenario = scenario,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                CurrentQuery = request.CurrentQuery,
                BuildStartTicks = DateTime.Now.Ticks,
            };

            var pawn = NpcManager.FindPawnByNpcId(request.NpcId);
            if (pawn == null && request.Map != null)
                pawn = NpcManager.FindProxyPawnForMap(request.Map!);
            var allKeys = ContextKeyRegistry.GetAll();

            var scenarioMeta = ScenarioRegistry.Get(request.Scenario ?? ScenarioIds.Dialogue);

            var excludeSet = new HashSet<string>();
            if (scenarioMeta?.DefaultExcludeKeys != null)
                excludeSet.UnionWith(scenarioMeta.DefaultExcludeKeys);
            if (request.ExcludeKeys != null)
                excludeSet.UnionWith(request.ExcludeKeys);

            var filteredKeys = allKeys.Where(k => !excludeSet.Contains(k.Key)).ToList();

            float budget = request.Budget > 0
                ? request.Budget
                : (scenarioMeta?.DefaultBudget > 0
                    ? scenarioMeta.DefaultBudget
                    : (RimMindCoreMod.Settings?.Context?.ContextBudget > 0
                        ? RimMindCoreMod.Settings.Context.ContextBudget
                        : 0.6f));
            var schedule = _scheduler.Schedule(filteredKeys, request.Scenario ?? ScenarioIds.Dialogue, budget, request.CurrentQuery);

            var allScheduledKeys = schedule.L0Keys.Concat(schedule.L1Keys)
                .Concat(schedule.L2Keys).Concat(schedule.L3Keys).Concat(schedule.L5Keys)
                .Select(k => k.Key).ToArray();
            var scheduledKeySet = new HashSet<string>(allScheduledKeys);
            var trimmedKeyNames = filteredKeys.Where(k => !scheduledKeySet.Contains(k.Key))
                .Select(k => k.Key).ToArray();
            snapshot.IncludedKeys = allScheduledKeys;
            snapshot.TrimmedKeys = trimmedKeyNames;
            snapshot.BudgetValue = budget;

            foreach (var key in schedule.L2Keys.Concat(schedule.L3Keys).Concat(schedule.L5Keys))
            {
                if (key.CurrentScore > 0)
                    snapshot.KeyScores[key.Key] = key.CurrentScore;
            }

            if (_diffTracker.DiffStore.TryGetValue(request.NpcId, out var diffs))
                snapshot.DiffCount = diffs.Count;

            var messages = new List<ChatMessage>();

            long l0Start = DateTime.Now.Ticks;
            var l0Msg = _layerBuilder.BuildL0(request.NpcId, request.Scenario ?? ScenarioIds.Dialogue, schedule.L0Keys, pawn, _cacheManager);
            if (l0Msg != null)
            {
                l0Msg.LayerTag = "L0";
                messages.Add(l0Msg);
                snapshot.Meta.L0Tokens = EstimateTokens(l0Msg.Content);
            }
            snapshot.LatencyByLayerMs["L0"] = (DateTime.Now.Ticks - l0Start) / TimeSpan.TicksPerMillisecond;

            long l1Start = DateTime.Now.Ticks;
            var l1Msg = _layerBuilder.BuildL1(request.NpcId, schedule.L1Keys, pawn, _cacheManager, _diffTracker);
            if (l1Msg != null)
            {
                l1Msg.LayerTag = "L1";
                messages.Add(l1Msg);
                snapshot.Meta.L1Tokens = EstimateTokens(l1Msg.Content);
            }
            snapshot.LatencyByLayerMs["L1"] = (DateTime.Now.Ticks - l1Start) / TimeSpan.TicksPerMillisecond;

            var mapStructureKey = schedule.L1Keys.FirstOrDefault(k => k.Key == "map_structure");
            if (mapStructureKey != null && pawn != null)
            {
                var mapEntries = mapStructureKey.ValueProvider(pawn);
                if (mapEntries != null) snapshot.AddEntries(mapEntries);
            }

            var l1DiffMsg = _layerBuilder.BuildDiffMessage(request.NpcId, ContextLayer.L1_Baseline, snapshot, _diffTracker);
            if (l1DiffMsg != null)
            {
                l1DiffMsg.LayerTag = "L1";
                messages.Add(l1DiffMsg);
            }

            long l2Start = DateTime.Now.Ticks;
            var l2Msg = _layerBuilder.BuildContextLayer(schedule.L2Keys, pawn);
            if (l2Msg != null)
            {
                var l2DiffMsg = _layerBuilder.BuildDiffMessage(request.NpcId, ContextLayer.L2_Environment, snapshot, _diffTracker);
                if (l2DiffMsg != null)
                {
                    l2DiffMsg.LayerTag = "L2";
                    messages.Add(l2DiffMsg);
                }
                l2Msg.LayerTag = "L2";
                messages.Add(l2Msg);
                snapshot.Meta.L2Tokens = EstimateTokens(l2Msg.Content);
            }
            snapshot.LatencyByLayerMs["L2"] = (DateTime.Now.Ticks - l2Start) / TimeSpan.TicksPerMillisecond;

            long l3Start = DateTime.Now.Ticks;
            var l3Msg = _layerBuilder.BuildContextLayer(schedule.L3Keys, pawn);
            if (l3Msg != null)
            {
                var l3DiffMsg = _layerBuilder.BuildDiffMessage(request.NpcId, ContextLayer.L3_State, snapshot, _diffTracker);
                if (l3DiffMsg != null)
                {
                    l3DiffMsg.LayerTag = "L3";
                    messages.Add(l3DiffMsg);
                }
                l3Msg.LayerTag = "L3";
                messages.Add(l3Msg);
                snapshot.Meta.L3Tokens = EstimateTokens(l3Msg.Content);
            }
            snapshot.LatencyByLayerMs["L3"] = (DateTime.Now.Ticks - l3Start) / TimeSpan.TicksPerMillisecond;

            long l5Start = DateTime.Now.Ticks;
            var l5Msg = _layerBuilder.BuildL5(schedule.L5Keys, pawn);
            if (l5Msg != null)
            {
                l5Msg.LayerTag = "L5";
                messages.Add(l5Msg);
                snapshot.Meta.L5Tokens = EstimateTokens(l5Msg.Content);
            }
            snapshot.LatencyByLayerMs["L5"] = (DateTime.Now.Ticks - l5Start) / TimeSpan.TicksPerMillisecond;

            int maxRounds = schedule.MaxHistoryRounds;
            var history = _historyManager.GetHistory(request.NpcId, maxRounds, scenario);
            foreach (var (role, content) in history)
            {
                messages.Add(new ChatMessage { Role = role, Content = content });
                snapshot.Meta.L4Tokens += EstimateTokens(content);
            }

            if (!string.IsNullOrEmpty(request.CurrentQuery))
            {
                string queryContent = !string.IsNullOrEmpty(request.SpeakerName)
                    ? "RimMind.Core.Prompt.Dialogue.SpeakerSays".Translate(request.SpeakerName!, PromptSanitizer.SanitizeUserInput(request.CurrentQuery!))
                    : PromptSanitizer.SanitizeUserInput(request.CurrentQuery!);
                messages.Add(new ChatMessage { Role = "user", Content = queryContent, LayerTag = "L4" });
            }

            bool hasUserMessage = messages.Any(m => m.Role == "user");
            if (!hasUserMessage)
            {
                string scenarioLabel = !string.IsNullOrEmpty(request.Scenario)
                    ? request.Scenario! : "general";
                messages.Add(new ChatMessage { Role = "user", Content = "RimMind.Core.Prompt.AutoAwait".Translate(scenarioLabel) });
            }

            snapshot.SetMessages(messages);
            snapshot.Meta.TotalTokens = snapshot.Meta.L0Tokens + snapshot.Meta.L1Tokens +
                snapshot.Meta.L2Tokens + snapshot.Meta.L3Tokens + snapshot.Meta.L4Tokens + snapshot.Meta.L5Tokens;
            snapshot.EstimatedTokens = snapshot.Meta.TotalTokens;

            ApplyBudgetTrim(snapshot);

            snapshot._commitFilteredKeys = filteredKeys;
            snapshot._commitSchedule = schedule;
            snapshot._commitPawn = pawn;

            return snapshot;
        }

        private void CommitSnapshotSideEffects(ContextSnapshot snapshot)
        {
            var pawn = snapshot._commitPawn as Pawn;
            var filteredKeys = snapshot._commitFilteredKeys;
            var schedule = snapshot._commitSchedule;

            _diffTracker.UpdateKeyValues(snapshot.NpcId, filteredKeys, pawn, _cacheManager, _scheduler);

            _diffTracker.MergeExpiredDiffs(snapshot.NpcId, _cacheManager.L1BlockCache, _cacheManager.L1Version, _cacheManager.L1KeyVersions);

            if (_cacheManager.PendingCacheEvents.Count > 0)
            {
                foreach (var kvp in _cacheManager.PendingCacheEvents)
                    snapshot.SetCacheHitEvent(kvp.Key, kvp.Value);
            }

            if (filteredKeys != null)
            {
                foreach (var key in filteredKeys)
                {
                    if (key.UpdateCount > 0)
                        snapshot.KeyChangeCounts[key.Key] = key.UpdateCount;
                }
            }

            if (schedule != null)
            {
                try
                {
                    var allSnapshotKeys = schedule.L0Keys.Concat(schedule.L1Keys)
                        .Concat(schedule.L2Keys).Concat(schedule.L3Keys).Concat(schedule.L5Keys);
                    foreach (var key in allSnapshotKeys)
                    {
                        string sourceText = "";
                        if (_diffTracker.KeyLastValues.TryGetValue(snapshot.NpcId, out var vals) &&
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
                catch (Exception ex) { Log.Warning($"[RimMind-Core] Embedding snapshot failed: {ex.Message}"); }
            }

            snapshot._commitFilteredKeys = null;
            snapshot._commitSchedule = null;
            snapshot._commitPawn = null;
        }

        internal void InvalidateLayer(string npcId, ContextLayer layer)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateLayer(npcId, layer);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        internal void InvalidateKey(string npcId, string key)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateKey(npcId, key);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        internal void UpdateBaseline(string npcId)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.UpdateBaseline(npcId);
                if (_diffTracker.DiffStore.TryGetValue(npcId, out var diffs))
                    diffs.Clear();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        internal void InvalidateNpc(string npcId)
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.InvalidateNpc(npcId);
                _diffTracker.DiffStore.Remove(npcId);
                _diffTracker.KeyLastValues.Remove(npcId);
                _historyManager.ClearHistory(npcId);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        private void ApplyBudgetTrim(ContextSnapshot snapshot)
        {
            if (snapshot.Messages == null || snapshot.Messages.Count == 0) return;

            int totalBudget = FlywheelParameterStore.Instance?.TotalBudget ?? 4000;
            int reserveForOutput = RimMindCoreMod.Settings?.maxTokens > 0
                ? RimMindCoreMod.Settings.maxTokens
                : 800;
            float budgetRatio = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f;
            int available = (int)(totalBudget * budgetRatio) - reserveForOutput;
            if (available <= 0) available = totalBudget - reserveForOutput;

            if (snapshot.EstimatedTokens <= available) return;

            var sections = new List<PromptSection>();
            foreach (var msg in snapshot.Messages)
            {
                int priority = msg.Role switch
                {
                    "system" when msg.LayerTag == "L0" => PromptSection.PriorityCore,
                    "system" => PromptSection.PriorityKeyState,
                    "user" => PromptSection.PriorityCurrentInput,
                    "assistant" => PromptSection.PriorityAuxiliary,
                    _ => PromptSection.PriorityAuxiliary
                };

                var section = new PromptSection(msg.Role ?? "unknown", msg.Content ?? "", priority)
                {
                    LayerTag = msg.LayerTag
                };

                if (msg.Role == "system" && (msg.LayerTag == "L2" || msg.LayerTag == "L3" || msg.LayerTag == "L5"))
                    section.Compress = CompressToBrief;

                sections.Add(section);
            }

            var budget = new PromptBudget(totalBudget, reserveForOutput);
            var trimmed = budget.Compose(sections);

            snapshot.ClearMessages();
            foreach (var sec in trimmed)
            {
                snapshot.AddMessage(new ChatMessage
                {
                    Role = sec.Tag,
                    Content = sec.Content,
                    LayerTag = sec.LayerTag
                });
            }
            snapshot.EstimatedTokens = trimmed.Sum(s => s.EstimatedTokens);
            snapshot.Meta.TotalTokens = snapshot.EstimatedTokens;

            if (RimMindCoreMod.Settings?.debugLogging == true)
            {
                Log.Message($"[RimMind-Core] Budget trim applied for {snapshot.NpcId}: trimmed to {snapshot.EstimatedTokens} tokens (budget: {available})");
            }
        }

        private static string CompressToBrief(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            const int briefLimitFallback = 200;
            int briefLimit = RimMindCoreMod.Settings?.Context?.contextBriefLimit ?? briefLimitFallback;
            if (content.Length <= briefLimit) return content;
            int cut = briefLimit;
            if (char.IsHighSurrogate(content[cut - 1])) cut--;
            return content.Substring(0, cut) + "...";
        }

        private static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int cjk = 0, other = 0;
            foreach (char c in text)
            {
                if (c > 0x2E80) cjk++;
                else other++;
            }
            return (int)(other / 4.0 + cjk / 1.5 + 0.5);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _rwLock?.Dispose();
        }

        public void ResetCaches()
        {
            if (_disposed) return;
            _rwLock.EnterWriteLock();
            try
            {
                _cacheManager.Reset();
                _diffTracker.Reset();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public int GetL0CacheCount()
        {
            _rwLock.EnterReadLock();
            try { return _cacheManager.GetL0CacheCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetL1BlockCacheCount()
        {
            _rwLock.EnterReadLock();
            try { return _cacheManager.GetL1BlockCacheCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetDiffStoreCount()
        {
            _rwLock.EnterReadLock();
            try { return _diffTracker.GetDiffStoreCount(); }
            finally { _rwLock.ExitReadLock(); }
        }

        public int GetEmbedCacheCount()
        {
            return _cacheManager.GetEmbedCacheCount();
        }
    }
}
