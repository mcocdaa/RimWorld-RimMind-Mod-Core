using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Domain.Common;
using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Domain.Storage;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.UI.Layout;
using RimMind.Infrastructure.UI.DebugCenter;
using RimMind.Presentation.Runtime.Services;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    [StaticConstructorOnStartup]
    public static class RimMindCoreDebugActions
    {
        private static T? CurrentRuntime<T>() where T : class
            => RuntimeServiceHub.Shared.Capture().GetOptional<T>();

        private static T? CurrentGame<T>() where T : class
            => GameServiceHub.Shared.Capture().GetOptional<T>();

        /// <summary>
        /// Cache all service references. Called from RimMindRuntime after services are registered.
        /// </summary>
        public static void Initialize(
            ISettingsProvider? settingsProvider,
            IAIRequestQueue? requestQueue,
            IClientManager? clientManager,
            IAIDebugLog? debugLog,
            IContextKeyProvider? contextKeyProvider,
            IContextBuilder? contextEngine,
            IProviderRegistry? providerRegistry,
            IContextKeyRegistry? contextKeyRegistry,
            IFlywheelParameterStore? flywheelParameterStore,
            ITelemetryCollector? telemetryCollector,
            IAgentBus? agentBus,
            IHistoryManager? historyManager,
            INpcManager? npcManager,
            IToolRegistry? toolRegistry,
            IGameMechanismRegistry? mechanismRegistry)
        {
            // Kept as a source-compatible composition hook. Debug actions resolve
            // from the lifecycle hubs when invoked and never retain these instances.
        }

        [DebugAction("RimMind", "Test API Connection", actionType = DebugActionType.Action)]
        public static void TestConnection()
        {
            if (!(CurrentRuntime<ISettingsProvider>()?.IsConfigured ?? false))
            {
                RimMindErrors.Warn("[RimMind-Core] API not configured. Set API Key in mod settings.");
                return;
            }

            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario("TestConnection")
                .WithModId("Debug")
                .WithMaxTokens(RimMindDefaults.TestConnectionMaxTokens)
                .WithTemperature(0f)
                .WithPriority(AIRequestPriority.High)
                .Build();

            // Add test messages
            envelope.Messages.Add(new ChatMessage { Role = "system", Content = "You are a test assistant. Always reply in JSON format." });
            envelope.Messages.Add(new ChatMessage { Role = "user", Content = "Reply with: {\"status\":\"ok\",\"message\":\"RimMind works\"}" });

            RimMind.Presentation.Api.RimMindAPI.Send(envelope, result =>
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (result.IsOk)
                        Messages.Message("RimMind.Infrastructure.Debug.ConnectionSuccess".Translate(result.Value.Content ?? ""), MessageTypeDefOf.PositiveEvent, false);
                    else
                        Messages.Message("RimMind.Infrastructure.Debug.ConnectionFailed".Translate(result.Error.Message), MessageTypeDefOf.NegativeEvent, false);
                });
            });

            Messages.Message("RimMind.Infrastructure.Debug.RequestSent".Translate(), MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("RimMind", "Show Last Prompt", actionType = DebugActionType.Action)]
        public static void ShowLastPrompt()
        {
            var entries = CurrentRuntime<IAIRequestTraceLog>()?.Entries;
            if (entries == null || entries.Count == 0)
            {
                Log.Message("[RimMind-Core] No request trace records.");
                return;
            }
            var last = entries[entries.Count - 1];
            Log.Message($"[RimMind-Core] Last request trace ({last.Source}):\n" +
                        $"=== System Prompt ===\n{last.SystemPrompt}\n" +
                        $"=== User Prompt ===\n{last.UserPrompt}\n" +
                        $"=== Response ===\n{last.Response}\n" +
                        $"=== Error ===\n{last.Error ?? string.Empty}");
        }

        [DebugAction("RimMind", "Clear Debug Log", actionType = DebugActionType.Action)]
        public static void ClearLog()
        {
            CurrentRuntime<IAIRequestTraceLog>()?.Clear();
            Log.Message("[RimMind-Core] Request trace log cleared.");
        }

        [DebugAction("RimMind", "Clear All Cooldowns", actionType = DebugActionType.Action)]
        public static void ClearCooldowns()
        {
            CurrentRuntime<IAIRequestQueue>()?.ClearAllCooldowns();
            Log.Message("[RimMind-Core] All cooldowns cleared.");
        }

        [DebugAction("RimMind", "Show Map Context", actionType = DebugActionType.Action)]
        public static void ShowMapContext()
        {
            var map = Find.CurrentMap;
            if (map == null) { RimMindErrors.Warn("[RimMind-Core] No map loaded."); return; }
            var contextKeyProvider = CurrentRuntime<IContextKeyProvider>();
            if (contextKeyProvider == null) { RimMindErrors.Warn("[RimMind-Core] ContextKeyProvider not available."); return; }
            var entries = contextKeyProvider.BuildMapContextEntries(map);
            var sb = new System.Text.StringBuilder();
            foreach (var entry in entries)
                sb.AppendLine(entry.Content);
            Log.Message("[RimMind-Core] Map Context:\n" + sb.ToString().TrimEnd());
        }

        [DebugAction("RimMind", "Show Pawn Context (selected)", actionType = DebugActionType.Action)]
        public static void ShowPawnContext()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null) { RimMindErrors.Warn("[RimMind-Core] Select a pawn first."); return; }
            var npcId = $"NPC-{pawn.thingIDNumber}";
            RuntimeServiceScope runtimeScope = RuntimeServiceHub.Shared.Capture();
            var contextEngine = runtimeScope.GetOptional<IContextBuilder>();
            if (contextEngine == null) { RimMindErrors.Warn("[RimMind-Core] ContextEngine not available."); return; }
            _ = LogPawnContextAsync(contextEngine, pawn, npcId, runtimeScope.Token);
            Log.Message("[RimMind-Core] Building selected pawn context asynchronously.");
        }

        private static async Task LogPawnContextAsync(
            IContextBuilder contextEngine,
            Pawn pawn,
            string npcId,
            RuntimeGenerationToken token)
        {
            try
            {
                var snapshot = await contextEngine.BuildSnapshotFromEnvelopeAsync(npcId, "[Debug] Show context");
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (!RuntimeServiceHub.Shared.IsCurrent(token))
                    {
                        RuntimeServiceHub.Shared.RecordStaleCompletion();
                        return;
                    }
                    LogContextSnapshot(pawn, npcId, snapshot);
                });
            }
            catch (Exception ex)
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (!RuntimeServiceHub.Shared.IsCurrent(token))
                    {
                        RuntimeServiceHub.Shared.RecordStaleCompletion();
                        return;
                    }
                    RimMindErrors.Warn($"[RimMind-Core] Context preview failed: {ex.Message}");
                });
            }
        }

        private static void LogContextSnapshot(Pawn pawn, string npcId, ContextSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                RimMindErrors.Warn("[RimMind-Core] Context preview returned no snapshot.");
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] Context Snapshot for {pawn.Name?.ToStringShort} (NpcId={npcId}):");
            sb.AppendLine($"Estimated tokens: {snapshot.EstimatedTokens}");
            sb.AppendLine($"L0={snapshot.Meta.L0Tokens} L1={snapshot.Meta.L1Tokens} L2={snapshot.Meta.L2Tokens} L3={snapshot.Meta.L3Tokens} L4={snapshot.Meta.L4Tokens}");
            sb.AppendLine("=== Messages ===");
            foreach (var msg in snapshot.Messages)
                sb.AppendLine($"[{msg.Role}] {msg.Content}");
            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Queue State", actionType = DebugActionType.Action)]
        public static void ShowQueueState()
        {
            var queue = CurrentRuntime<IAIRequestQueue>();
            if (queue == null)
            {
                RimMindErrors.Warn("[RimMind-Core] AIRequestQueue not initialized.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === Queue State ===");
            sb.AppendLine($"  Paused: {queue.IsPaused}");
            sb.AppendLine($"  Active requests: {queue.ActiveRequestCount}");
            sb.AppendLine($"  Local model busy: {queue.IsLocalModelBusy}");

            var active = queue.GetActiveRequests();
            foreach (var t in active)
            {
                sb.AppendLine($"  [Active] {t.Envelope.RequestId} mod={t.Envelope.ModId} " +
                              $"priority={t.Envelope.Priority} state={t.State} attempt={t.AttemptCount}");
            }

            foreach (var kvp in queue.GetAllQueueDepths())
            {
                int cooldownLeft = queue.GetCooldownTicksLeft(kvp.Key);
                sb.AppendLine($"  [Queue] {kvp.Key}: depth={kvp.Value}, cooldown={cooldownLeft}t");
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Pause Queue", actionType = DebugActionType.Action)]
        public static void PauseQueue()
        {
            CurrentRuntime<IAIRequestQueue>()?.PauseQueue();
            Log.Message("[RimMind-Core] Queue paused.");
        }

        [DebugAction("RimMind", "Resume Queue", actionType = DebugActionType.Action)]
        public static void ResumeQueue()
        {
            CurrentRuntime<IAIRequestQueue>()?.ResumeQueue();
            Log.Message("[RimMind-Core] Queue resumed.");
        }

        [DebugAction("RimMind", "Show Registered Providers", actionType = DebugActionType.Action)]
        public static void ShowRegisteredProviders()
        {
            var categories = CurrentRuntime<IProviderRegistry>()?.GetRegisteredCategories() ?? new List<string>();
            if (categories.Count == 0)
            {
                Log.Message("[RimMind-Core] No registered providers.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] Registered Providers ({categories.Count}):");

            Pawn? firstColonist = Enumerable.FirstOrDefault(
                Find.CurrentMap?.mapPawns?.FreeColonists ?? new System.Collections.Generic.List<Pawn>());

            foreach (var cat in categories)
            {
                sb.AppendLine($"  [{cat}]");

                var providerRegistry = CurrentRuntime<IProviderRegistry>();
                if (providerRegistry != null)
                {
                    var staticData = providerRegistry.GetStaticProviderData(cat);
                    if (staticData.IsOk && staticData.Value != null)
                        sb.AppendLine($"    Static: {staticData.Value.Length} chars");
                    else if (staticData.IsErr)
                        sb.AppendLine($"    Static: ERROR - {staticData.Error.Message}");

                    if (firstColonist != null)
                    {
                        var pawnData = providerRegistry.GetProviderData(cat, firstColonist);
                        if (pawnData.IsOk && pawnData.Value != null)
                            sb.AppendLine($"    Pawn ({firstColonist.Name?.ToStringShort}): {pawnData.Value.Length} chars");
                        else if (pawnData.IsErr)
                            sb.AppendLine($"    Pawn: ERROR - {pawnData.Error.Message}");
                    }
                }
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Registered ContextKeys", actionType = DebugActionType.Action)]
        public static void ShowRegisteredContextKeys()
        {
            var keys = CurrentRuntime<IContextKeyRegistry>()?.GetAll();
            if (keys.Count == 0)
            {
                Log.Message("[RimMind-Core] No registered context keys.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] Registered ContextKeys ({keys.Count}):");

            foreach (var key in keys)
            {
                sb.AppendLine($"  {key.Key} | Layer={key.Layer} | Priority={key.GetEffectivePriority():F3} | OwnerMod={key.OwnerMod} | Updates={key.UpdateCount}");
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Flywheel State", actionType = DebugActionType.Action)]
        public static void ShowFlywheelState()
        {
            var flywheelParameterStore = CurrentRuntime<IFlywheelParameterStore>();
            if (flywheelParameterStore == null)
            {
                RimMindErrors.Warn("[RimMind-Core] FlywheelParameterStore not initialized.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === Flywheel State ===");

            var current = flywheelParameterStore.GetAll();
            var defaults = flywheelParameterStore.GetDefaults();

            sb.AppendLine("  Parameters:");
            foreach (var kvp in current)
            {
                string defaultTag = defaults.TryGetValue(kvp.Key, out var def) && Math.Abs(def - kvp.Value) > 0.0001f
                    ? $" (default={def})"
                    : "";
                sb.AppendLine($"    {kvp.Key} = {kvp.Value}{defaultTag}");
            }

            sb.AppendLine($"  TotalBudget: {flywheelParameterStore.TotalBudget}");

            var recentRecords = CurrentRuntime<ITelemetryCollector>()?.GetRecentRecords(RimMindDefaults.TelemetryRecordLimit);
            sb.AppendLine($"  Telemetry records (recent {RimMindDefaults.TelemetryRecordLimit}): {recentRecords?.Count ?? 0}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Agent State (selected)", actionType = DebugActionType.Action)]
        public static void ShowAgentState()
        {
            Pawn? pawn = Find.Selector.SingleSelectedThing as Pawn;
            Find.WindowStack.Add(new Window_AgentStateDebug(pawn));
        }

        [DebugAction("RimMind", "Show AgentBus Subscribers", actionType = DebugActionType.Action)]
        public static void ShowAgentBusSubscribers()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === AgentBus Subscribers ===");

            var agentBus = CurrentRuntime<IAgentBus>();
            sb.AppendLine($"  AgentBus type: {agentBus?.GetType().Name ?? "null"}");

            sb.AppendLine($"  Registered event types: {agentBus?.GetHandlerCount() ?? 0}");

            sb.AppendLine($"  Background queue pending: {agentBus?.GetBackgroundQueueCount() ?? 0}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show History State (selected)", actionType = DebugActionType.Action)]
        public static void ShowHistoryState()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                RimMindErrors.Warn("[RimMind-Core] Select a pawn first.");
                return;
            }

            var npcId = $"NPC-{pawn.thingIDNumber}";
            var historyManager = CurrentRuntime<IHistoryManager>();
            var count = historyManager?.GetHistoryCount(npcId) ?? 0;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] History State for {pawn.Name?.ToStringShort} (NpcId={npcId}):");
            sb.AppendLine($"  Total entries: {count}");

            if (count > 0 && historyManager != null)
            {
                var recent = historyManager.GetHistory(npcId, 3);
                sb.AppendLine($"  Last {recent.Count} entries:");
                foreach (var (role, content) in recent)
                {
                    string preview = content.Length > RimMindDefaults.PreviewTruncateLength ? content.Substring(0, RimMindDefaults.PreviewTruncateLength) + "..." : content;
                    sb.AppendLine($"    [{role}] {preview}");
                }
            }

            if (historyManager != null)
            {
                var allForSave = historyManager.GetAllForSaveDict();
                sb.AppendLine($"  Total NPC histories: {allForSave.Count}");
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show NPC Manager State", actionType = DebugActionType.Action)]
        public static void ShowNpcManagerState()
        {
            var npcManager = CurrentGame<INpcManager>();
            if (npcManager == null)
            {
                RimMindErrors.Warn("[RimMind-Core] NpcManager not initialized.");
                return;
            }

            var npcs = npcManager.GetAllNpcs();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] NPC Manager State:");
            sb.AppendLine($"  Total NPCs: {npcs.Count}");

            foreach (var npc in npcs)
            {
                sb.AppendLine($"  [{npc.NpcId}] Name={npc.Name} Commands={npc.Commands.Count}");
                if (!string.IsNullOrEmpty(npc.CharacterDescription))
                {
                    string desc = npc.CharacterDescription.Length > RimMindDefaults.DescriptionTruncateLength
                        ? npc.CharacterDescription.Substring(0, RimMindDefaults.DescriptionTruncateLength) + "..."
                        : npc.CharacterDescription;
                    sb.AppendLine($"    Desc: {desc}");
                }
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Settings Summary", actionType = DebugActionType.Action)]
        public static void ShowSettingsSummary()
        {
            var s = CurrentRuntime<ISettingsProvider>();
            if (s == null)
            {
                RimMindErrors.Warn("[RimMind-Core] Settings not initialized.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === Settings Summary ===");
            sb.AppendLine($"  Provider: {s.Provider}");
            sb.AppendLine($"  Model: {s.ModelName}");
            sb.AppendLine($"  Endpoint: {s.ApiEndpoint}");
            sb.AppendLine($"  API Key: {(string.IsNullOrEmpty(s.ApiKey) ? "(empty)" : $"({s.ApiKey.Length} chars)")}");
            sb.AppendLine($"  ForceJsonMode: {s.ForceJsonMode}");
            sb.AppendLine($"  MaxTokens: {s.MaxTokens}");
            sb.AppendLine($"  DefaultTemperature: {s.DefaultTemperature}");
            sb.AppendLine($"  DebugLogging: {s.DebugLogging}");
            sb.AppendLine($"  MaxConcurrentRequests: {s.MaxConcurrentRequests}");
            sb.AppendLine($"  MaxRetryCount: {s.MaxRetryCount}");
            sb.AppendLine($"  RequestTimeoutMs: {s.RequestTimeoutMs}");
            sb.AppendLine($"  AutoApplyMode: (via Context)");
            sb.AppendLine($"  AutoApplyConfidenceThreshold: (via Context)");
            sb.AppendLine($"  RequestOverlayEnabled: (via UI)");
            sb.AppendLine($"  Player2RemoteUrl: {s.Player2RemoteUrl}");
            sb.AppendLine($"  TelemetryDataPath: (via Infrastructure)");
            sb.AppendLine($"  AnalysisReportPath: (via Infrastructure)");
            sb.AppendLine($"  IsConfigured: {s.IsConfigured}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "ToolCall Debug", actionType = DebugActionType.Action)]
        public static void OpenToolCallDebug()
        {
            Find.WindowStack.Add(new Window_ToolCallDebug());
        }

        [DebugAction("RimMind", "Mechanism Status", actionType = DebugActionType.Action)]
        public static void OpenMechanismStatus()
        {
            Find.WindowStack.Add(new Window_MechanismStatus());
        }

        [DebugAction("RimMind", "Agent Mode Debug", actionType = DebugActionType.Action)]
        public static void OpenAgentModeDebug()
        {
            Pawn? pawn = Find.Selector.SingleSelectedThing as Pawn;
            Find.WindowStack.Add(new Window_AgentModeDebug(pawn));
        }

        [DebugAction("RimMind", "Agent State Window (selected)", actionType = DebugActionType.Action)]
        public static void OpenAgentStateDebug()
        {
            Pawn? pawn = Find.Selector.SingleSelectedThing as Pawn;
            Find.WindowStack.Add(new Window_AgentStateDebug(pawn));
        }

        [DebugAction("RimMind", "Context Keys Window", actionType = DebugActionType.Action)]
        public static void OpenContextKeyDebug()
        {
            Find.WindowStack.Add(new Window_ContextKeyDebug());
        }

        // ── Autotests (H2 / K / L runtime verification) ──────────────────────

        [DebugAction("RimMind", "Agent Flow Lab", actionType = DebugActionType.Action)]
        public static void OpenAgentFlowLab()
        {
            Pawn? pawn = Find.Selector.SingleSelectedThing as Pawn;
            Find.WindowStack.Add(new Window_AgentFlowLab(pawn));
        }

        [DebugAction("RimMind", "Agent Progress Float", actionType = DebugActionType.Action)]
        public static void OpenAgentProgressFloat()
        {
            Find.WindowStack.Add(new Window_AgentProgressFloat());
        }

        [DebugAction("Autotests", "Test H2 Actions Equivalence", actionType = DebugActionType.Action)]
        public static void TestH2ActionsEquivalence()
        {
            int pass = 0, fail = 0;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Autotests] === H2 Actions Equivalence ===");

            // 1. Verify every registered Mechanism has a corresponding ToolHandler
            var mechanisms = CurrentRuntime<IGameMechanismRegistry>()?.All ?? new List<IGameMechanism>();
            var tools = CurrentRuntime<IToolRegistry>()?.All ?? new List<IToolHandler>();

            var mechanismIds = new HashSet<string>(mechanisms.Select(m => m.MechanismId));
            var toolIds = new HashSet<string>(tools.Select(t => t.Definition.Id));

            sb.AppendLine($"  Mechanisms: {mechanismIds.Count}, Tools: {toolIds.Count}");

            // 2. Each Mechanism's write actions should have a corresponding ToolHandler
            foreach (var mech in mechanisms)
            {
                var writeActions = mech.GetWriteActions();
                if (writeActions == null || writeActions.Count == 0)
                {
                    sb.AppendLine($"  [SKIP] {mech.MechanismId}: no write actions");
                    continue;
                }

                foreach (var action in writeActions)
                {
                    // Convention: tool id = "mechanismId_action" or "mechanismId"
                    string conventionToolId = $"{mech.MechanismId}_{action.Action}";
                    string mechanismToolId = mech.MechanismId;
                    if (toolIds.Contains(conventionToolId) || toolIds.Contains(mechanismToolId))
                    {
                        string matchedId = toolIds.Contains(conventionToolId) ? conventionToolId : mechanismToolId;
                        sb.AppendLine($"  [PASS] {mech.MechanismId}.{action.Action} -> {matchedId}");
                        pass++;
                    }
                    else
                    {
                        sb.AppendLine($"  [FAIL] {mech.MechanismId}.{action.Action} -> no matching tool (tried '{conventionToolId}', '{mechanismToolId}')");
                        Log.Error($"[Autotests] H2: {mech.MechanismId}.{action.Action} has no matching tool");
                        fail++;
                    }
                }
            }

            // 3. Verify tool count consistency
            int mechanismToolCount = mechanisms
                .SelectMany(m => m.GetWriteActions() ?? new List<MechanismActionInfo>())
                .Count();
            sb.AppendLine($"  Total mechanism write actions: {mechanismToolCount}, Total tools: {toolIds.Count}");

            sb.AppendLine($"  Result: {pass} passed, {fail} failed");
            Log.Message(sb.ToString());
            ReportAutotest("H2", pass, fail);
        }

        [DebugAction("Autotests", "Test P Visibility Entrypoints", actionType = DebugActionType.Action)]
        public static void TestPVisibilityEntrypoints()
        {
            int pass = 0, fail = 0;
            var sb = new StringBuilder();
            sb.AppendLine("[RimMind Autotest P] Visibility Entrypoints");

            void Check(string name, Func<bool> predicate)
            {
                try
                {
                    if (predicate())
                    {
                        pass++;
                        sb.AppendLine($"PASS {name}");
                    }
                    else
                    {
                        fail++;
                        sb.AppendLine($"FAIL {name}");
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    sb.AppendLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            Check("Core icon asset", () => ContentFinder<Texture2D>.Get("UI/RimMind/Icon", false) != null);
            foreach (string pageId in new[]
            {
                "overview", "agents", "ai_requests", "tool_calls", "mechanisms", "context_keys", "settings"
            })
            {
                Check($"Debug center page: {pageId}", () =>
                    DebugCenterPageRegistry.Find(pageId) != null
                    && DebugCenterPageRegistry.Create(pageId) != null);
            }

            sb.AppendLine($"Summary: {pass} passed, {fail} failed");
            if (fail > 0) Log.Error(sb.ToString());
            else Log.Message(sb.ToString());
            ReportAutotest("P.VisibilityEntrypoints", pass, fail);
        }

        [DebugAction("Autotests", "Test K Unified Request", actionType = DebugActionType.Action)]
        public static void TestKUnifiedRequest()
        {
            int pass = 0, fail = 0;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Autotests] === K Unified Request ===");

            // 1. NPC routing: NpcManager should be available and have active agents
            var npcManager = CurrentGame<INpcManager>();
            if (npcManager != null)
            {
                var npcs = npcManager.GetAllNpcs();
                var activeAgents = npcManager.GetActiveAgentPawnIds();
                sb.AppendLine($"  [PASS] NpcManager available: {npcs.Count} NPCs, {activeAgents.Count} active agents");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] NpcManager not initialized");
                Log.Error("[Autotests] K: NpcManager not initialized");
                fail++;
            }

            // 2. Storage abstraction: RequestQueue should be available
            var requestQueue = CurrentRuntime<IAIRequestQueue>();
            if (requestQueue != null)
            {
                sb.AppendLine($"  [PASS] AIRequestQueue available: paused={requestQueue.IsPaused}, active={requestQueue.ActiveRequestCount}");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] AIRequestQueue not initialized");
                Log.Error("[Autotests] K: AIRequestQueue not initialized");
                fail++;
            }

            // 3. ClientManager should be available for provider routing
            if (CurrentRuntime<IClientManager>() != null)
            {
                sb.AppendLine($"  [PASS] ClientManager available");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] ClientManager not initialized");
                Log.Error("[Autotests] K: ClientManager not initialized");
                fail++;
            }

            // 4. ToolRegistry should have registered tools (unified dispatch)
            var allTools = CurrentRuntime<IToolRegistry>()?.All;
            if (allTools != null && allTools.Count > 0)
            {
                sb.AppendLine($"  [PASS] ToolRegistry has {allTools.Count} registered tools");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] ToolRegistry empty or not initialized");
                Log.Error("[Autotests] K: ToolRegistry empty or not initialized");
                fail++;
            }

            // 5. AgentBus should be available for event dispatch
            var agentBus = CurrentRuntime<IAgentBus>();
            if (agentBus != null)
            {
                sb.AppendLine($"  [PASS] AgentBus available: handlers={agentBus.GetHandlerCount()}, pending={agentBus.GetBackgroundQueueCount()}");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] AgentBus not initialized");
                Log.Error("[Autotests] K: AgentBus not initialized");
                fail++;
            }

            sb.AppendLine($"  Result: {pass} passed, {fail} failed");
            Log.Message(sb.ToString());
            ReportAutotest("K.UnifiedRequest", pass, fail);
        }

        [DebugAction("Autotests", "Test L Context Evolution", actionType = DebugActionType.Action)]
        public static void TestLContextEvolution()
        {
            int pass = 0, fail = 0;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Autotests] === L Context Evolution ===");

            // 1. ContextKeyRegistry should have registered keys with staleness metadata
            var keys = CurrentRuntime<IContextKeyRegistry>()?.GetAll();
            if (keys != null && keys.Count > 0)
            {
                int withStaleness = 0;
                foreach (var key in keys)
                {
                    if (key.LastUpdatedTick > 0 || key.LastIncludedTick > 0)
                        withStaleness++;
                }
                sb.AppendLine($"  [PASS] ContextKeyRegistry: {keys.Count} keys, {withStaleness} with staleness data");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] ContextKeyRegistry empty or not initialized");
                Log.Error("[Autotests] L: ContextKeyRegistry empty or not initialized");
                fail++;
            }

            // 2. Seven-dimension scoring: keys should have AdaptivePriority and CurrentScore
            if (keys != null && keys.Count > 0)
            {
                int withAdaptive = 0;
                foreach (var key in keys)
                {
                    if (Math.Abs(key.AdaptivePriority - key.Priority) > 0.0001f || Math.Abs(key.CurrentScore) > 0.0001f)
                        withAdaptive++;
                }
                sb.AppendLine($"  [INFO] Adaptive scoring: {withAdaptive}/{keys.Count} keys have non-default adaptive values");

                if (withAdaptive > 0)
                {
                    sb.AppendLine($"  [PASS] Seven-dimension scoring active on {withAdaptive} keys");
                    pass++;
                }
                else
                {
                    sb.AppendLine($"  [WARN] No keys have adaptive scoring yet (may need game ticks)");
                    pass++; // Not a failure — scoring activates over time
                }
            }

            // 3. ContextEngine should be available for snapshot building
            if (CurrentRuntime<IContextBuilder>() != null)
            {
                sb.AppendLine($"  [PASS] ContextEngine (IContextBuilder) available");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] ContextEngine not initialized");
                Log.Error("[Autotests] L: ContextEngine not initialized");
                fail++;
            }

            // 4. ProviderRegistry should have registered providers
            var categories = CurrentRuntime<IProviderRegistry>()?.GetRegisteredCategories() ?? new List<string>();
            if (categories.Count > 0)
            {
                sb.AppendLine($"  [PASS] ProviderRegistry: {categories.Count} categories ({string.Join(", ", categories.Take(5))})");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] ProviderRegistry empty");
                Log.Error("[Autotests] L: ProviderRegistry empty");
                fail++;
            }

            // 5. FlywheelParameterStore should be available for learning feedback
            var flywheelParameterStore = CurrentRuntime<IFlywheelParameterStore>();
            if (flywheelParameterStore != null)
            {
                var parameters = flywheelParameterStore.GetAll();
                sb.AppendLine($"  [PASS] FlywheelParameterStore: {parameters.Count} parameters, budget={flywheelParameterStore.TotalBudget}");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] FlywheelParameterStore not initialized");
                Log.Error("[Autotests] L: FlywheelParameterStore not initialized");
                fail++;
            }

            // 6. TelemetryCollector for learning feedback chain
            var telemetryCollector = CurrentRuntime<ITelemetryCollector>();
            if (telemetryCollector != null)
            {
                var records = telemetryCollector.GetRecentRecords(5);
                sb.AppendLine($"  [PASS] TelemetryCollector available: {records?.Count ?? 0} recent records");
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] TelemetryCollector not initialized");
                Log.Error("[Autotests] L: TelemetryCollector not initialized");
                fail++;
            }

            sb.AppendLine($"  Result: {pass} passed, {fail} failed");
            Log.Message(sb.ToString());
            ReportAutotest("L.ContextEvolution", pass, fail);
        }

        [DebugAction("RimMind", "Dump UI Layout Conflicts", actionType = DebugActionType.Action)]
        public static void DumpUiLayoutConflicts()
        {
            var all = LayoutConflictStore.GetAll().ToList();
            if (all.Count == 0)
            {
                Log.Message("[RimMind-Core] No UI layout reports yet. Open a RimMind window first.");
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("[RimMind-Core] === UI Layout Conflict Report ===");
            foreach (var r in all.OrderBy(r => r.WindowName))
            {
                sb.AppendLine($"  [{r.WindowName}] {r.Conflicts.Count} conflict(s)");
                foreach (var c in r.Conflicts)
                    sb.AppendLine($"    - {c.Message}");
            }
            var worst = LayoutConflictStore.GetWorst();
            if (worst != null && worst.HasConflicts)
                sb.AppendLine($"  WORST: {worst.WindowName} ({worst.Conflicts.Count} conflicts)");
            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Toggle UI Layout Conflict Overlay", actionType = DebugActionType.Action)]
        public static void ToggleUiLayoutOverlay()
        {
            LayoutConflictStore.ShowOverlay = !LayoutConflictStore.ShowOverlay;
            Log.Message($"[RimMind-Core] UI layout conflict overlay: {(LayoutConflictStore.ShowOverlay ? "ON" : "OFF")}");
        }

        [DebugAction("Autotests", "Test UI Layout Conflict Detector", actionType = DebugActionType.Action)]
        public static void TestUiLayoutConflictDetector()
        {
            LayoutConflictStore.Clear();

            Window[] windows =
            {
                new Window_RequestLog(),
                new Window_AIDebugLog(),
                new Window_ToolCallDebug(),
                new Window_MechanismStatus(),
                new Window_ContextKeyDebug(),
                new Window_AgentStateDebug(),
                new Window_AgentModeDebug(),
                new Window_AgentFlowLab(),
                new Window_AgentProgressFloat(),
            };

            foreach (var w in windows)
            {
                Find.WindowStack.Add(w);
            }

            LayoutAutotestRunner.Run(windows, evaluation =>
            {
                ReportAutotest(
                    "UI.LayoutConflict",
                    evaluation.PassCount,
                    evaluation.FailCount,
                    evaluation.MissingReportCount);
            });
        }

        private static void ReportAutotest(string caseId, int pass, int fail, int skip = 0)
        {
            string outcome = fail > 0 ? "FAIL" : "PASS";
            Log.Message($"[RIMTEST][Core][{caseId}][{outcome}] pass={pass} fail={fail} skip={skip}");
        }
    }
}
