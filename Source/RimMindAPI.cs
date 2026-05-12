using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Context;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;
using RimMind.Contracts.Sensor;
using RimMind.Contracts.Tools;
using RimMind.Contracts.UI;
using RimMind.Adapters.Client.Player2;
using RimMind.Core.Agent;
using RimMind.Core.Runtime;
using RimMind.Kernel.Context;
using RimMind.Kernel.Flywheel;
using Verse;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static void Shutdown()
        {
            if (RimMindRuntime.Instance.IsShutdown) return;
            RimMindRuntime.Instance.Shutdown();
            RimMindRuntime.Instance.Queue?.CancelAllRequests();
        }

        internal static void ResetForNewGame() => RimMindRuntime.Instance.Reset();

        public static void RequestImmediate(AIRequest request, Action<Result<AIResponse, RimMindError>> onComplete)
            => Request.RequestImmediate(request, onComplete);
        public static void RequestStructuredAsync(AIRequest request, string? jsonSchema, Action<Result<AIResponse, RimMindError>> onComplete, List<StructuredTool>? tools = null)
            => Request.RequestStructuredAsync(request, jsonSchema, onComplete, tools);
        public static void RequestStructured(ContextRequest request, string schema, Action<Result<AIResponse, RimMindError>> onComplete, List<StructuredTool>? tools = null)
            => Request.RequestStructured(request, schema, onComplete, tools);
        public static void PauseQueue() => Request.PauseQueue();
        public static void ResumeQueue() => Request.ResumeQueue();
        public static int ActiveRequestCount => Request.ActiveRequestCount;
        public static IReadOnlyList<TrackedRequest> GetActiveRequests() => Request.GetActiveRequests();
        public static IReadOnlyList<TrackedRequest> GetAllQueuedRequests() => Request.GetAllQueuedRequests();
        public static int TotalQueuedCount => Request.TotalQueuedCount;
        public static void ClearModCooldown(string modId) => Request.ClearModCooldown(modId);

        public static Task<Result<NpcChatResult, RimMindError>> Chat(ContextRequest request, CancellationToken ct = default)
            => ChatFlow.Execute(request, ct);
        public static ContextSnapshot BuildContextSnapshot(ContextRequest request) => ChatFlow.BuildContextSnapshot(request);
        public static string BuildMapContext(Map map, bool brief = false) => ChatFlow.BuildMapContext(map, brief);

        public static IToolRegistry Tools => ToolSet.Registry;
        public static IGameMechanismRegistry Mechanisms => ToolSet.Mechanisms;

        public static IExtensionRegistry<T> Extensions<T>() where T : class, IExtension => Ext.Get<T>();
        public static bool ShouldSkipDialogue(Pawn pawn, string trigger) => Ext.ShouldSkipDialogue(pawn, trigger);
        public static bool ShouldSkipFloatMenu() => Ext.ShouldSkipFloatMenu();
        public static bool ShouldSkipAction(string intentId) => Ext.ShouldSkipAction(intentId);
        public static bool ShouldSkipStorytellerIncident() => Ext.ShouldSkipStorytellerIncident();
        public static void TriggerDialogue(Pawn pawn, string context, Pawn? recipient = null) => Ext.TriggerDialogue(pawn, context, recipient);
        public static void NotifyIncidentExecuted() => Ext.NotifyIncidentExecuted();
        public static bool CanTriggerDialogue => Ext.CanTriggerDialogue;
        public static void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider) => Ext.RegisterAgentIdentityProvider(provider);
        public static AgentIdentity? GetAgentIdentity(Pawn pawn) => Ext.GetAgentIdentity(pawn);
        public static void RegisterAgentActionBridge(IAgentActionBridge bridge) => Ext.RegisterAgentActionBridge(bridge);
        public static IAgentActionBridge? GetAgentActionBridge() => Ext.GetAgentActionBridge();
        public static void RegisterParameterTuner(IParameterTuner tuner) => Ext.RegisterParameterTuner(tuner);
        public static IReadOnlyList<IParameterTuner> ParameterTuners => Ext.ParameterTuners;
        public static void RegisterPawnContextProvider(string key, Func<Pawn, string?> provider, int priority = 8) => Ext.RegisterPawnContextProvider(key, provider, priority);

        public static Result<string?, RimMindError> GetProviderData(string category, Pawn pawn) => Providers.GetProviderData(category, pawn);
        public static Result<string?, RimMindError> GetStaticProviderData(string category) => Providers.GetStaticProviderData(category);
        public static List<string> GetRegisteredCategories() => Providers.GetRegisteredCategories();

        public static bool IsConfigured() => Settings.IsConfigured();
        internal static IHistoryManager GetHistoryManager() => Settings.GetHistoryManager();
        public static IContextEngine GetContextEngine() => Settings.GetContextEngine();
        internal static IBudgetScheduler? GetContextScheduler() => Settings.GetContextScheduler();
        internal static EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => Settings.GetEmbeddingSnapshotStore();
        public static FlywheelTelemetryCollector Telemetry => Settings.Telemetry;

        public static void RegisterSensorProvider(ISensorProvider provider) => Sensors.RegisterSensorProvider(provider);
        public static void UnregisterSensorProvider(string sensorId) => Sensors.UnregisterSensorProvider(sensorId);
        public static IReadOnlyList<ISensorProvider> SensorProviders => Sensors.SensorProviders;

        public static IAudioPlayer AudioPlayer => Audio.AudioPlayer;

        public static IEventBus GetEventBus() => Bus.GetEventBus();
        public static void PublishPerception(int pawnId, string type, string content, float importance = 0.5f) => Bus.PublishPerception(pawnId, type, content, importance);
        public static void RegisterPendingRequest(RequestEntry entry) => Bus.RegisterPendingRequest(entry);
        public static IReadOnlyList<RequestEntry> GetPendingRequests() => Bus.GetPendingRequests();
        internal static IAIClient? GetClient() => Bus.GetClient();
        public static void InvalidateClientCache() => Bus.InvalidateClientCache();
        public static Player2Client? GetPlayer2Client() => Bus.GetPlayer2Client();
    }
}
