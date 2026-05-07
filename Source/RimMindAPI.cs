﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Core.Client;
using RimMind.Kernel.Context;
using RimMind.Core.Extensions;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Runtime;
using RimMind.Core.Sensor;
using RimMind.Core.Settings;
using RimMind.Adapters.UI;
using RimMind.Core.UI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Prompt;
using RimWorld;
using Verse;

namespace RimMind.Core
{
    public static class RimMindAPI
    {
        public static void Shutdown()
        {
            if (RimMindRuntime.Instance.IsShutdown) return;
            RimMindRuntime.Instance.Shutdown();
            RimMindRuntime.Instance.Queue?.CancelAllRequests();
        }

        internal static void ResetForNewGame() => RimMindRuntime.Instance.Reset();

        internal static IHistoryManager GetHistoryManager() => RimMindRuntime.Instance.HistoryManager;
        public static IContextEngine GetContextEngine() => RimMindRuntime.Instance.ContextEngine;
        internal static IBudgetScheduler? GetContextScheduler() => RimMindRuntime.Instance.ContextEngine.GetScheduler();
        internal static EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => RimMindRuntime.Instance.ContextEngine.GetEmbeddingSnapshotStore();
        public static FlywheelTelemetryCollector Telemetry => RimMindRuntime.Instance.Telemetry;

        public static void RequestImmediate(AIRequest request, Action<AIResponse> onComplete)
        {
            var queue = RimMindRuntime.Instance.Queue;
            var client = GetClient();
            if (client == null)
            {
                onComplete?.Invoke(AIResponse.Failure(request.RequestId, "AI client not configured."));
                return;
            }
            queue.EnqueueImmediate(request, onComplete, client);
        }

        public static void PauseQueue() => RimMindRuntime.Instance.Queue?.PauseQueue();
        public static void ResumeQueue() => RimMindRuntime.Instance.Queue?.ResumeQueue();
        public static int ActiveRequestCount => RimMindRuntime.Instance.Queue?.ActiveRequestCount ?? 0;

        public static IReadOnlyList<TrackedRequest> GetActiveRequests()
            => RimMindRuntime.Instance.Queue?.GetActiveRequests() ?? new List<TrackedRequest>();

        public static IReadOnlyList<TrackedRequest> GetAllQueuedRequests()
            => RimMindRuntime.Instance.Queue?.GetAllQueuedRequests() ?? new List<TrackedRequest>();

        public static int TotalQueuedCount => RimMindRuntime.Instance.Queue?.TotalQueuedCount ?? 0;

        public static async Task<NpcChatResult> Chat(ContextRequest request, CancellationToken ct = default)
        {
            if (RimMindRuntime.Instance.IsShutdown) return new NpcChatResult { Error = "RimMind is shut down." };
            try
            {
                var driver = StorageDriverFactory.GetDriver();
                if (!driver.IsNpcAlive(request.NpcId) && request.NpcId.StartsWith("NPC-")
                    && int.TryParse(request.NpcId.Substring(4), out _))
                {
                    var npcMgr = RimMindServiceLocator.Get<INpcManager>();
                    var pawn = npcMgr?.FindPawnByNpcId(request.NpcId);
                    if (pawn != null)
                    {
                        var profile = NpcProfileBuilder.BuildPawnNpc(pawn);
                        await driver.SpawnNpcAsync(profile);
                        LongEventHandler.ExecuteWhenFinished(() => npcMgr?.SpawnNpc(profile));
                    }
                }

                var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(request);
                return await driver.ChatAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                return new NpcChatResult { Error = ex.Message };
            }
        }

        public static void RequestStructuredAsync(AIRequest request, string? jsonSchema, Action<AIResponse> onComplete, List<StructuredTool>? tools = null)
        {
            var s = RimMindCoreMod.Settings;
            if (s == null || !s.IsConfigured())
            {
                onComplete?.Invoke(AIResponse.Failure(request.RequestId, "AI client not configured."));
                return;
            }

            request.UseJsonMode = true;
            if (!string.IsNullOrEmpty(jsonSchema))
                request.JsonSchema = jsonSchema;
            if (tools != null && tools.Count > 0)
                request.Tools = tools;

            var queue = RimMindRuntime.Instance.Queue;
            var client = GetClient();
            if (client == null)
            {
                onComplete?.Invoke(AIResponse.Failure(request.RequestId, "AI client not available."));
                return;
            }

            queue.Enqueue(request, onComplete, client);
        }

        public static ContextSnapshot BuildContextSnapshot(ContextRequest request)
            => RimMindRuntime.Instance.ContextEngine.BuildSnapshot(request);

        public static void RequestStructured(ContextRequest request, string schema,
            Action<AIResponse> onComplete, List<StructuredTool>? tools = null)
        {
            if (RimMindRuntime.Instance.IsShutdown)
            {
                onComplete?.Invoke(AIResponse.Failure($"Structured_{request.NpcId}", "RimMind is shut down."));
                return;
            }

            var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(request);
            var aiRequest = new AIRequest
            {
                SystemPrompt = string.Empty,
                Messages = new List<ChatMessage>(snapshot.Messages),
                MaxTokens = snapshot.MaxTokens, Temperature = snapshot.Temperature,
                RequestId = $"Structured_{request.NpcId}", ModId = request.Scenario.ToString(),
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true, Priority = AIRequestPriority.Normal,
            };

            Action<AIResponse> wrappedOnComplete = (response) =>
            {
                try
                {
                    bool parseSuccess = response.Success && !string.IsNullOrEmpty(response.Content);
                    Telemetry.Record(new TelemetryRecord
                    {
                        NpcId = request.NpcId, Scenario = request.Scenario,
                        PromptTokens = response.PromptTokens, CompletionTokens = response.CompletionTokens,
                        TotalTokens = response.TokensUsed, CachedTokens = response.CachedTokens,
                        BudgetValue = snapshot.BudgetValue,
                        KeysIncluded = snapshot.IncludedKeys, KeysTrimmed = snapshot.TrimmedKeys,
                        LayerTokenBreakdown = new Dictionary<string, int>
                        {
                            { "L0", snapshot.Meta.L0Tokens }, { "L1", snapshot.Meta.L1Tokens },
                            { "L2", snapshot.Meta.L2Tokens }, { "L3", snapshot.Meta.L3Tokens },
                            { "L4", snapshot.Meta.L4Tokens }, { "L5", snapshot.Meta.L5Tokens },
                        },
                        KeyChangeFreq = snapshot.KeyChangeCounts.Count > 0 ? new Dictionary<string, int>(snapshot.KeyChangeCounts) : null,
                        ScoreDistribution = snapshot.KeyScores.Count > 0 ? new Dictionary<string, float>(snapshot.KeyScores) : null,
                        DiffCount = snapshot.DiffCount,
                        LatencyByLayerMs = snapshot.LatencyByLayerMs.Count > 0 ? new Dictionary<string, long>(snapshot.LatencyByLayerMs) : null,
                        RequestLatencyMs = snapshot.BuildStartTicks > 0 ? (DateTime.Now.Ticks - snapshot.BuildStartTicks) / TimeSpan.TicksPerMillisecond : 0,
                        ResponseParseSuccess = parseSuccess, TimestampTicks = DateTime.Now.Ticks,
                    });
                }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] Telemetry record failed: {ex.Message}"); }
                onComplete?.Invoke(response);
            };

            try
            {
                RequestStructuredAsync(aiRequest, schema, wrappedOnComplete, tools);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimMind-Core] RequestStructuredAsync threw, falling back to queue: {ex.Message}");
                Log.Warning($"[RimMind-Core] Structured request failed, falling back to plain request for {request.NpcId}");
                var fallbackRequest = new AIRequest
                {
                    SystemPrompt = string.Empty,
                    Messages = new List<ChatMessage>(snapshot.Messages),
                    MaxTokens = snapshot.MaxTokens, Temperature = snapshot.Temperature,
                    RequestId = aiRequest.RequestId, ModId = aiRequest.ModId,
                    ExpireAtTicks = aiRequest.ExpireAtTicks,
                    UseJsonMode = true, Priority = aiRequest.Priority,
                };
                var queue = RimMindRuntime.Instance.Queue;
                var client = GetClient();
                if (client != null)
                    queue.Enqueue(fallbackRequest, wrappedOnComplete, client);
                else
                    wrappedOnComplete?.Invoke(AIResponse.Failure(fallbackRequest.RequestId, "No AI client available"));
            }
        }

        public static string BuildMapContext(Map map, bool brief = false)
            => GameContextBuilder.BuildMapContext(map, brief);

        public static bool IsConfigured() => RimMindCoreMod.Settings.IsConfigured();

        public static string? GetProviderData(string category, Pawn pawn)
            => RimMindRuntime.Instance.ProviderRegistry.GetProviderData(category, pawn);

        public static string? GetStaticProviderData(string category)
            => RimMindRuntime.Instance.ProviderRegistry.GetStaticProviderData(category);

        public static List<string> GetRegisteredCategories()
            => RimMindRuntime.Instance.ProviderRegistry.GetRegisteredCategories();

        public static IEventBus GetEventBus() => RimMindRuntime.Instance.EventBus;

        public static IExtensionRegistry<T> Extensions<T>() where T : class, IExtension
            => RimMindRuntime.Instance.GetExtensionRegistry<T>();

        public static bool ShouldSkipDialogue(Pawn pawn, string trigger)
            => Extensions<ISkipCheck>().All
                .Where(s => s.Kind == SkipCheckKind.Dialogue)
                .Any(s => s.ShouldSkip(new SkipCheckArgs { Pawn = pawn, Trigger = trigger }));

        public static bool ShouldSkipFloatMenu()
            => Extensions<ISkipCheck>().All
                .Where(s => s.Kind == SkipCheckKind.FloatMenu)
                .Any(s => s.ShouldSkip(default));

        public static bool ShouldSkipAction(string intentId)
            => Extensions<ISkipCheck>().All
                .Where(s => s.Kind == SkipCheckKind.Action)
                .Any(s => s.ShouldSkip(new SkipCheckArgs { IntentId = intentId }));

        public static bool ShouldSkipStorytellerIncident()
            => Extensions<ISkipCheck>().All
                .Where(s => s.Kind == SkipCheckKind.StorytellerIncident)
                .Any(s => s.ShouldSkip(default));

        public static void TriggerDialogue(Pawn pawn, string context, Pawn? recipient = null)
        {
            foreach (var t in Extensions<IDialogueTrigger>().All)
                t.Trigger(pawn, context, recipient);
        }

        public static void NotifyIncidentExecuted()
        {
            foreach (var l in Extensions<IIncidentExecutedListener>().All)
                l.OnIncidentExecuted();
        }

        public static bool CanTriggerDialogue
            => Extensions<IDialogueTrigger>().All.Any();

        public static void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => RimMindRuntime.Instance.RegisterAgentIdentityProvider(provider);

        public static AgentIdentity? GetAgentIdentity(Pawn pawn)
            => RimMindRuntime.Instance.GetAgentIdentity(pawn);

        public static void RegisterAgentActionBridge(IAgentActionBridge bridge)
            => RimMindRuntime.Instance.RegisterAgentActionBridge(bridge);

        public static IAgentActionBridge? GetAgentActionBridge()
            => RimMindRuntime.Instance.GetAgentActionBridge();

        public static IAudioPlayer AudioPlayer => RimMindRuntime.Instance.AudioPlayer;

        public static void PublishPerception(int pawnId, string type, string content, float importance = 0.5f)
            => Perception.PerceptionBridge.PublishPerception(pawnId, type, content, importance, GetEventBus());

        public static void RegisterPendingRequest(RequestEntry entry)
            => RimMindRuntime.Instance.OverlayService.RegisterPendingRequest(entry);

        public static IReadOnlyList<RequestEntry> GetPendingRequests()
            => RimMindRuntime.Instance.OverlayService.GetPendingRequests();

        internal static IAIClient? GetClient()
            => RimMindRuntime.Instance.GetClient();

        public static void InvalidateClientCache()
            => RimMindRuntime.Instance.InvalidateClientCache();

        public static Player2Client? GetPlayer2Client()
            => RimMindRuntime.Instance.GetPlayer2Client();

        public static void RegisterParameterTuner(IParameterTuner tuner)
            => RimMindRuntime.Instance.RegisterParameterTuner(tuner);

        public static IReadOnlyList<IParameterTuner> ParameterTuners
            => RimMindRuntime.Instance.ParameterTunersList;

        public static void RegisterSensorProvider(ISensorProvider provider)
            => RimMindRuntime.Instance.RegisterSensorProvider(provider);

        public static void UnregisterSensorProvider(string sensorId) => RimMindRuntime.Instance.UnregisterSensorProvider(sensorId);
        public static IReadOnlyList<ISensorProvider> SensorProviders => RimMindRuntime.Instance.SensorProvidersList;

        public static void ClearModCooldown(string modId) => RimMindRuntime.Instance.Queue?.ClearCooldown(modId);
    }
}
