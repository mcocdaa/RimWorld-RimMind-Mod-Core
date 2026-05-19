using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using LudeonTK;
using RimWorld;
using Verse;

namespace RimMind.Infrastructure.UI
{
    [StaticConstructorOnStartup]
    public static class RimMindCoreDebugActions
    {
        [DebugAction("RimMind", "Test API Connection", actionType = DebugActionType.Action)]
        public static void TestConnection()
        {
            if (!(RimMindServiceLocator.Get<ISettingsProvider>()?.IsConfigured ?? false))
            {
                RimMindErrors.Warn("[RimMind-Core] API not configured. Set API Key in mod settings.");
                return;
            }

            var request = new AIRequest
            {
                SystemPrompt = "You are a test assistant. Always reply in JSON format.",
                UserPrompt = "Reply with: {\"status\":\"ok\",\"message\":\"RimMind works\"}",
                MaxTokens = 60,
                Temperature = 0f,
                RequestId = "Debug_TestConnection",
                ModId = "Debug",
                ExpireAtTicks = Find.TickManager.TicksGame + 3600,
                Priority = AIRequestPriority.High,
            };

            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
            var client = RimMindServiceLocator.Get<IClientManager>()?.GetClient();
            if (queue == null || client == null)
            {
                Messages.Message("RimMind.Infrastructure.Debug.ConnectionFailed".Translate("Queue or client not available"), MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            queue.EnqueueImmediate(request, response =>
            {
                if (response.State == AIRequestState.Completed)
                    Messages.Message("RimMind.Infrastructure.Debug.ConnectionSuccess".Translate(response.Content), MessageTypeDefOf.PositiveEvent, false);
                else
                    Messages.Message("RimMind.Infrastructure.Debug.ConnectionFailed".Translate(response.State.ToString()), MessageTypeDefOf.NegativeEvent, false);
            }, client);

            Messages.Message("RimMind.Infrastructure.Debug.RequestSent".Translate(), MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("RimMind", "Show Last Prompt", actionType = DebugActionType.Action)]
        public static void ShowLastPrompt()
        {
            var entries = RimMindServiceLocator.Get<IAIDebugLog>()?.Entries;
            if (entries == null || entries.Count == 0)
            {
                Log.Message("[RimMind-Core] No request records.");
                return;
            }
            var last = entries[entries.Count - 1];
            Log.Message($"[RimMind-Core] Last request ({last.Source}):\n" +
                        $"=== System Prompt ===\n{last.FullSystemPrompt}\n" +
                        $"=== User Prompt ===\n{last.FullUserPrompt}\n" +
                        $"=== Response ===\n{last.FullResponse}");
        }

        [DebugAction("RimMind", "Clear Debug Log", actionType = DebugActionType.Action)]
        public static void ClearLog()
        {
            RimMindServiceLocator.Get<IAIDebugLog>()?.Clear();
            Log.Message("[RimMind-Core] Debug log cleared.");
        }

        [DebugAction("RimMind", "Clear All Cooldowns", actionType = DebugActionType.Action)]
        public static void ClearCooldowns()
        {
            RimMindServiceLocator.Get<IAIRequestQueue>()?.ClearAllCooldowns();
            Log.Message("[RimMind-Core] All cooldowns cleared.");
        }

        [DebugAction("RimMind", "Show Map Context", actionType = DebugActionType.Action)]
        public static void ShowMapContext()
        {
            var map = Find.CurrentMap;
            if (map == null) { RimMindErrors.Warn("[RimMind-Core] No map loaded."); return; }
            var ctxProvider = RimMindServiceLocator.Get<IContextKeyProvider>();
            if (ctxProvider == null) { RimMindErrors.Warn("[RimMind-Core] ContextKeyProvider not available."); return; }
            var entries = ctxProvider.BuildMapContextEntries(map);
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
            var request = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Dialogue,
                Budget = 0.6f,
                CurrentQuery = "[Debug] Show context",
            };
            var engine = RimMindServiceLocator.Get<IContextEngine>();
            var snapshot = engine.BuildSnapshot(request);
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
            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
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
                sb.AppendLine($"  [Active] {t.Request.RequestId} mod={t.Request.ModId} " +
                              $"priority={t.Request.Priority} state={t.State} attempt={t.AttemptCount}");
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
            RimMindServiceLocator.Get<IAIRequestQueue>()?.PauseQueue();
            Log.Message("[RimMind-Core] Queue paused.");
        }

        [DebugAction("RimMind", "Resume Queue", actionType = DebugActionType.Action)]
        public static void ResumeQueue()
        {
            RimMindServiceLocator.Get<IAIRequestQueue>()?.ResumeQueue();
            Log.Message("[RimMind-Core] Queue resumed.");
        }

        [DebugAction("RimMind", "Show Registered Providers", actionType = DebugActionType.Action)]
        public static void ShowRegisteredProviders()
        {
            var categories = RimMindServiceLocator.Get<IProviderRegistry>()?.GetRegisteredCategories() ?? new List<string>();
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

                var reg = RimMindServiceLocator.Get<IProviderRegistry>();
                if (reg != null)
                {
                    var staticData = reg.GetStaticProviderData(cat);
                    if (staticData.IsOk && staticData.Value != null)
                        sb.AppendLine($"    Static: {staticData.Value.Length} chars");
                    else if (staticData.IsErr)
                        sb.AppendLine($"    Static: ERROR - {staticData.Error.Message}");

                    if (firstColonist != null)
                    {
                        var pawnData = reg.GetProviderData(cat, firstColonist);
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
            var keys = RimMindServiceLocator.Get<IContextKeyRegistry>()?.GetAll();
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
            var store = RimMindServiceLocator.Get<IFlywheelParameterStore>();
            if (store == null)
            {
                RimMindErrors.Warn("[RimMind-Core] FlywheelParameterStore not initialized.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === Flywheel State ===");

            var current = store.GetAll();
            var defaults = store.GetDefaults();

            sb.AppendLine("  Parameters:");
            foreach (var kvp in current)
            {
                string defaultTag = defaults.TryGetValue(kvp.Key, out var def) && Math.Abs(def - kvp.Value) > 0.0001f
                    ? $" (default={def})"
                    : "";
                sb.AppendLine($"    {kvp.Key} = {kvp.Value}{defaultTag}");
            }

            sb.AppendLine($"  TotalBudget: {store.TotalBudget}");

            var telemetry = RimMindServiceLocator.Get<ITelemetryCollector>();
            var recentRecords = telemetry.GetRecentRecords(100);
            sb.AppendLine($"  Telemetry records (recent 100): {recentRecords?.Count ?? 0}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Agent State (selected)", actionType = DebugActionType.Action)]
        public static void ShowAgentState()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                RimMindErrors.Warn("[RimMind-Core] Select a pawn first.");
                return;
            }

            var comp = RimMind.Infrastructure.Verse.CompPawnAgent.GetComp(pawn);
            if (comp == null || comp.Agent == null)
            {
                RimMindErrors.Warn($"[RimMind-Core] {pawn.Name?.ToStringShort} has no PawnAgent comp.");
                return;
            }

            var agent = comp.Agent;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] Agent State for {pawn.Name?.ToStringShort}:");
            sb.AppendLine(agent.GetDebugInfo());

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show AgentBus Subscribers", actionType = DebugActionType.Action)]
        public static void ShowAgentBusSubscribers()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RimMind-Core] === AgentBus Subscribers ===");

            var agentBus = RimMindServiceLocator.Get<IAgentBus>();
            sb.AppendLine($"  AgentBus type: {agentBus?.GetType().Name ?? "null"}");

            sb.AppendLine($"  Registered event types: {agentBus.GetHandlerCount()}");

            sb.AppendLine($"  Background queue pending: {agentBus.GetBackgroundQueueCount()}");

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
            var history = RimMindServiceLocator.Get<IHistoryManager>();
            var count = history.GetHistoryCount(npcId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] History State for {pawn.Name?.ToStringShort} (NpcId={npcId}):");
            sb.AppendLine($"  Total entries: {count}");

            if (count > 0)
            {
                var recent = history.GetHistory(npcId, 3);
                sb.AppendLine($"  Last {recent.Count} entries:");
                foreach (var (role, content) in recent)
                {
                    string preview = content.Length > 120 ? content.Substring(0, 120) + "..." : content;
                    sb.AppendLine($"    [{role}] {preview}");
                }
            }

            var allForSave = history.GetAllForSaveDict();
            sb.AppendLine($"  Total NPC histories: {allForSave.Count}");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show NPC Manager State", actionType = DebugActionType.Action)]
        public static void ShowNpcManagerState()
        {
            var mgr = RimMindServiceLocator.Get<INpcManager>();
            if (mgr == null)
            {
                RimMindErrors.Warn("[RimMind-Core] NpcManager not initialized.");
                return;
            }

            var npcs = mgr.GetAllNpcs();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RimMind-Core] NPC Manager State:");
            sb.AppendLine($"  Total NPCs: {npcs.Count}");

            foreach (var npc in npcs)
            {
                sb.AppendLine($"  [{npc.NpcId}] Name={npc.Name} Commands={npc.Commands.Count}");
                if (!string.IsNullOrEmpty(npc.CharacterDescription))
                {
                    string desc = npc.CharacterDescription.Length > 80
                        ? npc.CharacterDescription.Substring(0, 80) + "..."
                        : npc.CharacterDescription;
                    sb.AppendLine($"    Desc: {desc}");
                }
            }

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind", "Show Settings Summary", actionType = DebugActionType.Action)]
        public static void ShowSettingsSummary()
        {
            var s = RimMindServiceLocator.Get<ISettingsProvider>();
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
    }
}
