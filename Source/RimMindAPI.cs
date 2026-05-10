using RimMind.Contracts.Npc;
using RimMind.Core.Sensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Core;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Client;
using RimMind.Contracts.Context;
using RimMind.Kernel.Context;
using RimMind.Contracts.Extensions;
using RimMind.Contracts.Internal;
using RimMind.Core.Runtime;
using RimMind.Adapters.UI;
using RimMind.Contracts.UI;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Pipeline;
using RimMind.Kernel.Prompt;
using RimMind.Adapters.Client.Player2;
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
                var ctx = new NpcChatContext { Request = request, Ct = ct };
                await RimMindRuntime.Instance.NpcChatPipeline.ExecuteAsync(ctx);
                return ctx.Result ?? new NpcChatResult { Error = ctx.IsShortCircuited ? ctx.ShortCircuitReason : "Pipeline produced no result." };
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
            if (!string.IsNullOrEmpty(schema)) aiRequest.JsonSchema = schema;
            if (tools != null && tools.Count > 0) aiRequest.Tools = tools;

            var ctx = new AIRequestContext { Request = aiRequest, Client = GetClient(), Snapshot = snapshot };
            aiRequest.TraceId = ctx.TraceId;
            RimMindRuntime.Instance.AIRequestPipeline.ExecuteAsync(ctx).ContinueWith(_ =>
            {
                onComplete?.Invoke(ctx.Response ?? AIResponse.Failure(aiRequest.RequestId, "Pipeline failed"));
            }, TaskContinuationOptions.ExecuteSynchronously);
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

        public static void RegisterPawnContextProvider(string key, Func<Pawn, string?> provider, int priority = 8)
        {
            ContextKeyRegistry.Register(key, ContextLayer.L4_History, priority / 10f,
                pawnObj =>
                {
                    var p = pawnObj as Pawn;
                    if (p == null) return new List<ContextEntry>();
                    var value = provider(p);
                    return string.IsNullOrEmpty(value) ? new List<ContextEntry>() : new List<ContextEntry> { new ContextEntry(value) };
                }, "External");
        }
    }
}
