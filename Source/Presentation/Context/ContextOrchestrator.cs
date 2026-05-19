using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Prompt;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Context;
using Verse;

namespace RimMind.Presentation.Context
{
    public class ContextOrchestrator : IContextEngine
    {
        private bool _needsFullRebuild = true;
        private bool _disposed;

        private readonly IHistoryManager _historyManager;
        private readonly INpcManager _npcManager;
        private readonly IContextCacheManager _cacheManager;
        private readonly IContextDiffTracker _diffTracker;
        private readonly IContextLayerBuilder _layerBuilder;
        private readonly IBudgetScheduler _scheduler;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ITranslationService _translationService;
        private readonly IFlywheelParameterStore _flywheelParameterStore;
        private readonly ILogSink _logSink;
        private readonly EmbeddingSnapshotStore _embeddingSnapshotStore = new EmbeddingSnapshotStore();

        public ContextOrchestrator(
            IHistoryManager historyManager,
            INpcManager npcManager,
            IContextCacheManager cacheManager,
            IContextDiffTracker diffTracker,
            IContextLayerBuilder layerBuilder,
            IBudgetScheduler scheduler,
            ISettingsProvider settingsProvider,
            ITranslationService translationService,
            IFlywheelParameterStore flywheelParameterStore,
            ILogSink logSink)
        {
            _historyManager = historyManager;
            _npcManager = npcManager;
            _cacheManager = cacheManager;
            _diffTracker = diffTracker;
            _layerBuilder = layerBuilder;
            _scheduler = scheduler;
            _settingsProvider = settingsProvider;
            _translationService = translationService;
            _flywheelParameterStore = flywheelParameterStore;
            _logSink = logSink;
        }

        public ContextSnapshot? BuildSnapshot(ContextRequest request)
        {
            if (_disposed) return null;
            string scenario = request.Scenario ?? ScenarioIds.Dialogue;
            string l0CacheKey = $"{request.NpcId}_{scenario}";
            _cacheManager.TouchCache(l0CacheKey);
            ContextKeyRegistry.CurrentScenario = scenario;

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

            var pawn = _npcManager?.FindPawnByNpcId(request.NpcId);
            if (pawn == null && request.Map != null)
                pawn = _npcManager?.FindProxyPawnForMap((Verse.Map)request.Map!);
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
                    : (_settingsProvider?.Context?.ContextBudget > 0
                        ? _settingsProvider!.Context!.ContextBudget
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

            if (_diffTracker.TryGetDiffStore(request.NpcId, out var diffs))
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
                var translationService = _translationService;
                string queryContent = !string.IsNullOrEmpty(request.SpeakerName)
                    ? translationService?.Translate("RimMind.Presentation.Prompt.Dialogue.SpeakerSays", request.SpeakerName!, PromptSanitizer.SanitizeUserInput(request.CurrentQuery!))
                        ?? $"[{request.SpeakerName}]: {PromptSanitizer.SanitizeUserInput(request.CurrentQuery!)}"
                    : PromptSanitizer.SanitizeUserInput(request.CurrentQuery!);
                messages.Add(new ChatMessage { Role = "user", Content = queryContent, LayerTag = "L4" });
            }

            bool hasUserMessage = messages.Any(m => m.Role == "user");
            if (!hasUserMessage)
            {
                string scenarioLabel = !string.IsNullOrEmpty(request.Scenario)
                    ? request.Scenario! : "general";
                var translationService = _translationService;
                string autoAwaitContent = translationService?.Translate("RimMind.Presentation.Prompt.AutoAwait", scenarioLabel)
                    ?? $"[AutoAwait: {scenarioLabel}]";
                messages.Add(new ChatMessage { Role = "user", Content = autoAwaitContent });
            }

            snapshot.SetMessages(messages);
            snapshot.Meta.TotalTokens = snapshot.Meta.L0Tokens + snapshot.Meta.L1Tokens +
                snapshot.Meta.L2Tokens + snapshot.Meta.L3Tokens + snapshot.Meta.L4Tokens + snapshot.Meta.L5Tokens;
            snapshot.EstimatedTokens = snapshot.Meta.TotalTokens;

            ApplyBudgetTrim(snapshot);

            snapshot._commitPayload = new CommitPayload
            {
                FilteredKeys = filteredKeys,
                Schedule = schedule,
                Pawn = pawn
            };

            return snapshot;
        }

        private void ApplyBudgetTrim(ContextSnapshot snapshot)
        {
            if (snapshot.Messages == null || snapshot.Messages.Count == 0) return;

            int totalBudget = _flywheelParameterStore?.TotalBudget ?? 4000;
            int reserveForOutput = _settingsProvider?.MaxTokens > 0
                ? _settingsProvider!.MaxTokens
                : 800;
            float budgetRatio = _settingsProvider?.Context?.ContextBudget ?? 0.6f;
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
            var trimmed = budget.Compose(sections) ?? new List<PromptSection>();

            snapshot.ClearMessages();
            foreach (var sec in trimmed)
            {
                snapshot.AddMessage(new ChatMessage
                {
                    Role = sec.Name,
                    Content = sec.Content,
                    LayerTag = sec.LayerTag
                });
            }
            snapshot.EstimatedTokens = trimmed.Sum(s => s.EstimatedTokens);
            snapshot.Meta.TotalTokens = snapshot.EstimatedTokens;

            if (_settingsProvider?.DebugLogging == true)
            {
                _logSink?.Message($"Budget trim applied for {snapshot.NpcId}: trimmed to {snapshot.EstimatedTokens} tokens (budget: {available})");
            }
        }

        private string CompressToBrief(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            const int briefLimitFallback = 200;
            int briefLimit = _settingsProvider?.Context?.ContextBriefLimit ?? briefLimitFallback;
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

        public int GetL0CacheCount() => _cacheManager.GetL0CacheCount();
        public int GetL1BlockCacheCount() => _cacheManager.GetL1BlockCacheCount();
        public int GetDiffStoreCount() => _diffTracker.GetDiffStoreCount();
        public int GetEmbedCacheCount() => _cacheManager.GetEmbedCacheCount();
        public void ResetCaches() { _cacheManager.Reset(); _diffTracker.Reset(); _needsFullRebuild = true; }
        public void TouchCache(string cacheKey) => _cacheManager.TouchCache(cacheKey);
        public void RemoveL0CacheForNpc(string npcId) => _cacheManager.RemoveL0CacheForNpc(npcId);
        public void InvalidateLayer(string npcId, ContextLayer layer) => _cacheManager.InvalidateLayer(npcId, layer);
        public void InvalidateKey(string npcId, string key) => _cacheManager.InvalidateKey(npcId, key);
        public void UpdateBaseline(string npcId) { _cacheManager.UpdateBaseline(npcId); if (_diffTracker.TryGetDiffStore(npcId, out var diffs)) diffs.Clear(); }
        public void InvalidateNpc(string npcId) { _cacheManager.InvalidateNpc(npcId); _diffTracker.ClearNpcDiffs(npcId); _diffTracker.RemoveNpcKeyLastValues(npcId); _historyManager.ClearHistory(npcId); _needsFullRebuild = true; }
        public IBudgetScheduler? GetScheduler() => _scheduler;
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => _embeddingSnapshotStore;
        public void Dispose() { _disposed = true; }
    }
}
