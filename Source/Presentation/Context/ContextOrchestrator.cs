using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Prompt;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Presentation.Context
{
    public class ContextOrchestrator : IContextEngine
    {
        private const float DefaultContextBudget = RimMindDefaults.DefaultContextBudget;
        private const int DefaultTotalBudget = RimMindDefaults.DefaultTotalBudget;
        private const int DefaultReserveForOutput = RimMindDefaults.DefaultReserveForOutput;
        private const int DefaultBriefLimit = RimMindDefaults.DefaultBriefLimit;
        private const int CjkCharacterThreshold = 0x2E80;
        private const float TokenEstimateOther = RimMindDefaults.TokenEstimateMultiplier;
        private const float TokenEstimateCjk = RimMindDefaults.TokenEstimateDivider;
        private const float TokenEstimateOverhead = RimMindDefaults.TokenEstimateMinRatio;

        private readonly record struct BuildContext(
            string NpcId,
            string Scenario,
            float Budget,
            string? CurrentQuery,
            string[]? ExcludeKeys,
            int MaxTokens,
            float Temperature,
            object? Map,
            string? SpeakerName,
            int MaxRounds,
            bool IsMonologue
        );

        private bool _needsFullRebuild = true;
        private bool _disposed;

        private readonly IHistoryManager _historyManager;
        private readonly INpcManager? _npcManager;
        private readonly ContextBuildServices _buildServices;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ITranslationService _translationService;
        private readonly IFlywheelParameterStore _flywheelParameterStore;
        private readonly ILogSink _logSink;
        private readonly EmbeddingSnapshotStore _embeddingSnapshotStore;
        private readonly IContextKeyRegistry _keyRegistry;
        private readonly IRelevanceTable _relevanceTable;
        private readonly ProviderCache? _providerCache;
        private readonly ITickProvider? _tickProvider;

        public ContextOrchestrator(
            IHistoryManager historyManager,
            INpcManager? npcManager,
            ContextBuildServices buildServices,
            ISettingsProvider settingsProvider,
            ITranslationService translationService,
            IFlywheelParameterStore flywheelParameterStore,
            ILogSink logSink,
            EmbeddingSnapshotStore embeddingSnapshotStore,
            IContextKeyRegistry keyRegistry,
            IRelevanceTable relevanceTable,
            ProviderCache? providerCache = null,
            ITickProvider? tickProvider = null)
        {
            _historyManager = historyManager;
            _npcManager = npcManager;
            _buildServices = buildServices;
            _settingsProvider = settingsProvider;
            _translationService = translationService;
            _flywheelParameterStore = flywheelParameterStore;
            _logSink = logSink;
            _embeddingSnapshotStore = embeddingSnapshotStore;
            _keyRegistry = keyRegistry;
            _relevanceTable = relevanceTable;
            _providerCache = providerCache;
            _tickProvider = tickProvider;
        }

        private ContextSnapshot? BuildSnapshotInternal(BuildContext ctx)
        {
            if (_disposed) return null;
            string scenario = ctx.Scenario;
            string l0CacheKey = $"{ctx.NpcId}_{scenario}";
            _buildServices.CacheManager.TouchCache(l0CacheKey);

            if (_needsFullRebuild)
            {
                _buildServices.DiffTracker.RemoveNpcKeyLastValues(ctx.NpcId);
                _buildServices.DiffTracker.ClearNpcDiffs(ctx.NpcId);
                _needsFullRebuild = false;
            }

            var snapshot = new ContextSnapshot
            {
                NpcId = ctx.NpcId,
                Scenario = scenario,
                MaxTokens = ctx.MaxTokens,
                Temperature = ctx.Temperature,
                CurrentQuery = ctx.CurrentQuery,
                BuildStartTicks = DateTime.Now.Ticks,
            };

            var pawn = _npcManager?.FindPawnByNpcId(ctx.NpcId);
            if (pawn == null && ctx.Map != null)
                pawn = _npcManager?.FindProxyPawnForMap((Verse.Map)ctx.Map!);

            var (schedule, filteredKeys, budget) = ScheduleBudget(ctx, scenario, pawn);
            PopulateSnapshotKeys(snapshot, schedule, filteredKeys);
            snapshot.BudgetValue = budget;

            var messages = new List<ChatMessage>();

            BuildL0Layer(ctx, schedule, pawn, snapshot, messages);
            BuildL1Layer(ctx, schedule, pawn, snapshot, messages);
            BuildL2Layer(ctx, schedule, pawn, snapshot, messages);
            BuildL3Layer(ctx, schedule, pawn, snapshot, messages);
            BuildL5Layer(schedule, pawn, snapshot, messages);
            BuildConversationHistory(ctx, schedule, scenario, messages, snapshot);

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

        public ContextSnapshot? BuildSnapshotFromEnvelope(string npcId, string? currentQuery, int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null)
        {
            var ctx = new BuildContext(
                NpcId: npcId,
                Scenario: scenarioId ?? ScenarioIds.Dialogue,
                Budget: 0,
                CurrentQuery: currentQuery,
                ExcludeKeys: null,
                MaxTokens: maxTokens,
                Temperature: temperature,
                Map: null,
                SpeakerName: null,
                MaxRounds: 0,
                IsMonologue: false
            );
            return BuildSnapshotInternal(ctx);
        }

        /// <summary>
        /// Async version of BuildSnapshotFromEnvelope with parallel Provider execution.
        /// All layer builds run in parallel via Task.WhenAll. Each Provider is wrapped
        /// in try-catch so a single failure does not affect others.
        /// </summary>
        public async Task<ContextSnapshot?> BuildSnapshotFromEnvelopeAsync(string npcId, string? currentQuery,
            int maxTokens = 800, float temperature = 0.7f, string? scenarioId = null,
            HashSet<string>? skipLayers = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (_disposed) return null;

            var ctx = new BuildContext(
                NpcId: npcId,
                Scenario: scenarioId ?? ScenarioIds.Dialogue,
                Budget: 0,
                CurrentQuery: currentQuery,
                ExcludeKeys: null,
                MaxTokens: maxTokens,
                Temperature: temperature,
                Map: null,
                SpeakerName: null,
                MaxRounds: 0,
                IsMonologue: false
            );

            string scenario = ctx.Scenario;
            string l0CacheKey = $"{ctx.NpcId}_{scenario}";
            _buildServices.CacheManager.TouchCache(l0CacheKey);

            if (_needsFullRebuild)
            {
                _buildServices.DiffTracker.RemoveNpcKeyLastValues(ctx.NpcId);
                _buildServices.DiffTracker.ClearNpcDiffs(ctx.NpcId);
                _needsFullRebuild = false;
            }

            var snapshot = new ContextSnapshot
            {
                NpcId = ctx.NpcId,
                Scenario = scenario,
                MaxTokens = ctx.MaxTokens,
                Temperature = ctx.Temperature,
                CurrentQuery = ctx.CurrentQuery,
                BuildStartTicks = DateTime.Now.Ticks,
            };

            var pawn = _npcManager?.FindPawnByNpcId(ctx.NpcId);
            if (pawn == null && ctx.Map != null)
                pawn = _npcManager?.FindProxyPawnForMap((Verse.Map)ctx.Map!);

            var (schedule, filteredKeys, budget) = ScheduleBudget(ctx, scenario, pawn);
            PopulateSnapshotKeys(snapshot, schedule, filteredKeys);
            snapshot.BudgetValue = budget;

            // Build ProviderContext for async provider calls
            var providerCtx = new ProviderContext(ctx.Scenario, Domain.ValueObjects.TraceContext.Current ?? "")
            {
                PawnId = (pawn as Verse.Pawn)?.thingIDNumber ?? 0,
                NpcId = ctx.NpcId,
                MapId = (pawn as Verse.Pawn)?.Map?.uniqueID,
            };

            // Execute all layer builds in parallel via BuildLayerAsync
            var l0Task = _buildServices.LayerBuilder.BuildLayerAsync(schedule.L0Keys, pawn, providerCtx, _providerCache, ct)
                .ContinueWith(t => t.IsFaulted ? new List<ContextEntry>() : t.Result, ct);
            var l1Task = _buildServices.LayerBuilder.BuildLayerAsync(schedule.L1Keys, pawn, providerCtx, _providerCache, ct)
                .ContinueWith(t => t.IsFaulted ? new List<ContextEntry>() : t.Result, ct);
            var l2Task = _buildServices.LayerBuilder.BuildLayerAsync(schedule.L2Keys, pawn, providerCtx, _providerCache, ct)
                .ContinueWith(t => t.IsFaulted ? new List<ContextEntry>() : t.Result, ct);
            var l3Task = (skipLayers != null && skipLayers.Contains("L3"))
                ? Task.FromResult(new List<ContextEntry>())
                : _buildServices.LayerBuilder.BuildLayerAsync(schedule.L3Keys, pawn, providerCtx, _providerCache, ct)
                    .ContinueWith(t => t.IsFaulted ? new List<ContextEntry>() : t.Result, ct);
            var l5Task = _buildServices.LayerBuilder.BuildLayerAsync(schedule.L5Keys, pawn, providerCtx, _providerCache, ct)
                .ContinueWith(t => t.IsFaulted ? new List<ContextEntry>() : t.Result, ct);

            var layerResults = await Task.WhenAll(l0Task, l1Task, l2Task, l3Task, l5Task).ConfigureAwait(false);

            // Convert entries to ChatMessages
            var l0Msg = _buildServices.LayerBuilder.EntriesToLayerMessage(layerResults[0], "L0");
            var l1Msg = _buildServices.LayerBuilder.EntriesToLayerMessage(layerResults[1], "L1");
            var l2Msg = _buildServices.LayerBuilder.EntriesToLayerMessage(layerResults[2], "L2");
            var l3Msg = _buildServices.LayerBuilder.EntriesToLayerMessage(layerResults[3], "L3");
            var l5Msg = _buildServices.LayerBuilder.EntriesToLayerMessage(layerResults[4], "L5");

            // Merge results into snapshot in layer order
            var messages = new List<ChatMessage>();
            if (l0Msg != null) { messages.Add(l0Msg); snapshot.Meta.L0Tokens = EstimateTokens(l0Msg.Content); }
            if (l1Msg != null) { messages.Add(l1Msg); snapshot.Meta.L1Tokens = EstimateTokens(l1Msg.Content); }
            if (l2Msg != null) { messages.Add(l2Msg); snapshot.Meta.L2Tokens = EstimateTokens(l2Msg.Content); }
            if (l3Msg != null && (skipLayers == null || !skipLayers.Contains("L3"))) { messages.Add(l3Msg); snapshot.Meta.L3Tokens = EstimateTokens(l3Msg.Content); }
            if (l5Msg != null) { messages.Add(l5Msg); snapshot.Meta.L5Tokens = EstimateTokens(l5Msg.Content); }

            BuildConversationHistory(ctx, schedule, scenario, messages, snapshot);

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

        private void BuildL0Layer(BuildContext ctx, BudgetAllocation schedule, object? pawn, ContextSnapshot snapshot, List<ChatMessage> messages)
        {
            long l0Start = DateTime.Now.Ticks;
            var l0Msg = _buildServices.LayerBuilder.BuildL0(ctx.NpcId, ctx.Scenario, schedule.L0Keys, pawn, _buildServices.CacheManager);
            if (l0Msg != null)
            {
                l0Msg.LayerTag = "L0";
                messages.Add(l0Msg);
                snapshot.Meta.L0Tokens = EstimateTokens(l0Msg.Content);
            }
            snapshot.LatencyByLayerMs["L0"] = (DateTime.Now.Ticks - l0Start) / TimeSpan.TicksPerMillisecond;
        }

        private void BuildL1Layer(BuildContext ctx, BudgetAllocation schedule, object? pawn, ContextSnapshot snapshot, List<ChatMessage> messages)
        {
            long l1Start = DateTime.Now.Ticks;
            var l1Msg = _buildServices.LayerBuilder.BuildL1(ctx.NpcId, schedule.L1Keys, pawn, _buildServices.CacheManager, _buildServices.DiffTracker);
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

            var l1DiffMsg = _buildServices.LayerBuilder.BuildDiffMessage(ctx.NpcId, ContextLayer.L1_Baseline, snapshot, _buildServices.DiffTracker);
            if (l1DiffMsg != null)
            {
                l1DiffMsg.LayerTag = "L1";
                messages.Add(l1DiffMsg);
            }
        }

        private void BuildL2Layer(BuildContext ctx, BudgetAllocation schedule, object? pawn, ContextSnapshot snapshot, List<ChatMessage> messages)
        {
            long l2Start = DateTime.Now.Ticks;
            var l2Msg = _buildServices.LayerBuilder.BuildContextLayer(schedule.L2Keys, pawn);
            if (l2Msg != null)
            {
                var l2DiffMsg = _buildServices.LayerBuilder.BuildDiffMessage(ctx.NpcId, ContextLayer.L2_Environment, snapshot, _buildServices.DiffTracker);
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
        }

        private void BuildL3Layer(BuildContext ctx, BudgetAllocation schedule, object? pawn, ContextSnapshot snapshot, List<ChatMessage> messages)
        {
            long l3Start = DateTime.Now.Ticks;
            var l3Msg = _buildServices.LayerBuilder.BuildL3(schedule.L3Keys, pawn);
            if (l3Msg != null)
            {
                var l3DiffMsg = _buildServices.LayerBuilder.BuildDiffMessage(ctx.NpcId, ContextLayer.L3_State, snapshot, _buildServices.DiffTracker);
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
        }

        private void BuildL5Layer(BudgetAllocation schedule, object? pawn, ContextSnapshot snapshot, List<ChatMessage> messages)
        {
            long l5Start = DateTime.Now.Ticks;
            var l5Msg = _buildServices.LayerBuilder.BuildL5(schedule.L5Keys, pawn);
            if (l5Msg != null)
            {
                l5Msg.LayerTag = "L5";
                messages.Add(l5Msg);
                snapshot.Meta.L5Tokens = EstimateTokens(l5Msg.Content);
            }
            snapshot.LatencyByLayerMs["L5"] = (DateTime.Now.Ticks - l5Start) / TimeSpan.TicksPerMillisecond;
        }

        private void BuildConversationHistory(BuildContext ctx, BudgetAllocation schedule, string scenario, List<ChatMessage> messages, ContextSnapshot snapshot)
        {
            int maxRounds = schedule.MaxHistoryRounds;
            var history = _historyManager.GetHistory(ctx.NpcId, maxRounds, scenario);
            foreach (var (role, content) in history)
            {
                messages.Add(new ChatMessage { Role = role, Content = content });
                snapshot.Meta.L4Tokens += EstimateTokens(content);
            }

            if (!string.IsNullOrEmpty(ctx.CurrentQuery))
            {
                var translationService = _translationService;
                string queryContent = !string.IsNullOrEmpty(ctx.SpeakerName)
                    ? translationService?.Translate("RimMind.Prompt.Dialogue.SpeakerSays", ctx.SpeakerName!, PromptSanitizer.SanitizeUserInput(ctx.CurrentQuery!))
                        ?? $"[{ctx.SpeakerName}]: {PromptSanitizer.SanitizeUserInput(ctx.CurrentQuery!)}"
                    : PromptSanitizer.SanitizeUserInput(ctx.CurrentQuery!);
                messages.Add(new ChatMessage { Role = "user", Content = queryContent, LayerTag = "L4" });
            }

            bool hasUserMessage = messages.Any(m => m.Role == "user");
            if (!hasUserMessage)
            {
                string scenarioLabel = !string.IsNullOrEmpty(ctx.Scenario)
                    ? ctx.Scenario : "general";
                var translationService = _translationService;
                string autoAwaitContent = translationService?.Translate("RimMind.Prompt.AutoAwait", scenarioLabel)
                    ?? $"[AutoAwait: {scenarioLabel}]";
                messages.Add(new ChatMessage { Role = "user", Content = autoAwaitContent });
            }
        }

        private (BudgetAllocation schedule, List<KeyMeta> filteredKeys, float budget) ScheduleBudget(BuildContext ctx, string scenario, object? pawn)
        {
            var allKeys = _keyRegistry.GetAll();

            var scenarioMeta = ScenarioRegistry.Get(ctx.Scenario);

            var excludeSet = new HashSet<string>();
            if (scenarioMeta?.DefaultExcludeKeys != null)
                excludeSet.UnionWith(scenarioMeta.DefaultExcludeKeys);
            if (ctx.ExcludeKeys != null)
                excludeSet.UnionWith(ctx.ExcludeKeys);

            var filteredKeys = allKeys.Where(k => !excludeSet.Contains(k.Key)).ToList();

            float budget = ctx.Budget > 0
                ? ctx.Budget
                : (scenarioMeta?.DefaultBudget > 0
                    ? scenarioMeta.DefaultBudget
                    : (_settingsProvider?.Context?.ContextBudget > 0
                        ? _settingsProvider.Context.ContextBudget
                        : DefaultContextBudget));

            var sctx = new ScoringContext
            {
                Scenario = scenario,
                NowTicks = _tickProvider?.TicksGame ?? 0,
                Query = ctx.CurrentQuery,
                UserPinnedKeys = new HashSet<string>()
            };
            var schedule = _buildServices.BudgetScheduler.ScheduleWithContext(filteredKeys, sctx, budget);

            return (schedule, filteredKeys, budget);
        }

        private void PopulateSnapshotKeys(ContextSnapshot snapshot, BudgetAllocation schedule, List<KeyMeta> filteredKeys)
        {
            var allScheduledKeys = schedule.L0Keys.Concat(schedule.L1Keys)
                .Concat(schedule.L2Keys).Concat(schedule.L3Keys).Concat(schedule.L5Keys)
                .Select(k => k.Key).ToArray();
            var scheduledKeySet = new HashSet<string>(allScheduledKeys);
            var trimmedKeyNames = filteredKeys.Where(k => !scheduledKeySet.Contains(k.Key))
                .Select(k => k.Key).ToArray();
            snapshot.IncludedKeys = allScheduledKeys;
            snapshot.TrimmedKeys = trimmedKeyNames;

            foreach (var key in schedule.L2Keys.Concat(schedule.L3Keys).Concat(schedule.L5Keys))
            {
                if (key.CurrentScore > 0)
                    snapshot.KeyScores[key.Key] = key.CurrentScore;
            }

            if (_buildServices.DiffTracker.TryGetDiffStore(snapshot.NpcId, out var diffs))
                snapshot.DiffCount = diffs.Count;
        }

        private void ApplyBudgetTrim(ContextSnapshot snapshot)
        {
            if (snapshot.Messages == null || snapshot.Messages.Count == 0) return;

            int totalBudget = _flywheelParameterStore?.TotalBudget ?? DefaultTotalBudget;
            int reserveForOutput = _settingsProvider?.MaxTokens > 0
                ? _settingsProvider!.MaxTokens
                : DefaultReserveForOutput;
            float budgetRatio = _settingsProvider?.Context?.ContextBudget ?? DefaultContextBudget;
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
            const int briefLimitFallback = DefaultBriefLimit;
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
                if (c > CjkCharacterThreshold) cjk++;
                else other++;
            }
            return (int)(other / TokenEstimateOther + cjk / TokenEstimateCjk + TokenEstimateOverhead);
        }

        public int GetL0CacheCount() => _buildServices.CacheManager.GetL0CacheCount();
        public int GetL1BlockCacheCount() => _buildServices.CacheManager.GetL1BlockCacheCount();
        public int GetDiffStoreCount() => _buildServices.DiffTracker.GetDiffStoreCount();
        public int GetEmbedCacheCount() => _buildServices.CacheManager.GetEmbedCacheCount();
        public void ResetCaches() { _buildServices.CacheManager.Reset(); _buildServices.DiffTracker.Reset(); _needsFullRebuild = true; }
        public void TouchCache(string cacheKey) => _buildServices.CacheManager.TouchCache(cacheKey);
        public void RemoveL0CacheForNpc(string npcId) => _buildServices.CacheManager.RemoveL0CacheForNpc(npcId);
        public void InvalidateLayer(string npcId, ContextLayer layer) => _buildServices.CacheManager.InvalidateLayer(npcId, layer);
        public void InvalidateKey(string npcId, string key) => _buildServices.CacheManager.InvalidateKey(npcId, key);
        public void UpdateBaseline(string npcId) { _buildServices.CacheManager.UpdateBaseline(npcId); if (_buildServices.DiffTracker.TryGetDiffStore(npcId, out var diffs)) diffs.Clear(); }
        public void InvalidateNpc(string npcId) { _buildServices.CacheManager.InvalidateNpc(npcId); _buildServices.DiffTracker.ClearNpcDiffs(npcId); _buildServices.DiffTracker.RemoveNpcKeyLastValues(npcId); _historyManager.ClearHistory(npcId); _needsFullRebuild = true; }
        public IBudgetScheduler? GetScheduler() => _buildServices.BudgetScheduler;
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => _embeddingSnapshotStore;
        public void Dispose() { _disposed = true; }
    }
}
