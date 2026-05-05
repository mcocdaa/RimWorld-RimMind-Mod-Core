using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Core.Agent;
using RimMind.Core.AgentBus;
using RimMind.Core.Client;
using RimMind.Core.Context;
using RimMind.Core.Extensions;
using RimMind.Core.Flywheel;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Prompt;
using RimMind.Core.Settings;
using RimMind.Core.UI;
using RimWorld;
using Verse;

namespace RimMind.Core
{
    public static class RimMindAPI
    {
        private static volatile bool _isShutdown;

        private static readonly ProviderRegistry _providerRegistry = new ProviderRegistry();
        private static readonly SkipCheckRegistry _skipCheckRegistry = new SkipCheckRegistry();
        private static readonly ClientManager _clientManager = new ClientManager();
        private static readonly OverlayService _overlayService = new OverlayService();
        private static readonly IncidentRegistry _incidentRegistry = new IncidentRegistry();

        private static readonly ConcurrentDictionary<string, (string tabId, Func<string> labelFn, Action<UnityEngine.Rect> drawFn)>
            _settingsTabs = new ConcurrentDictionary<string, (string, Func<string>, Action<UnityEngine.Rect>)>();

        private static readonly ConcurrentDictionary<string, (string id, Func<bool> isActive, Action toggle)>
            _toggleBehaviors = new ConcurrentDictionary<string, (string, Func<bool>, Action)>();

        private static readonly ConcurrentDictionary<string, Func<int>> _modCooldownGetters
            = new ConcurrentDictionary<string, Func<int>>();

        private static volatile Action<Pawn, string, Pawn?>? _dialogueTriggerFn;

        private static readonly IEventBus _eventBus = new EventBusAdapter();
        private static readonly HistoryManager _historyManager = HistoryManager.Instance;
        private static readonly ContextEngine _contextEngine = new ContextEngine(_historyManager);
        private static volatile Func<Pawn, AgentIdentity?>? _agentIdentityProvider;
        private static IAgentActionBridge? _agentActionBridge;
        private static IAudioPlayer _audioPlayer = new NullAudioPlayer();
        private static readonly ConcurrentDictionary<string, IParameterTuner> _parameterTuners
            = new ConcurrentDictionary<string, IParameterTuner>();
        private static readonly ConcurrentDictionary<string, ISensorProvider> _sensorProviders
            = new ConcurrentDictionary<string, ISensorProvider>();
        private static int _callbackCounter;

        public static void Shutdown()
        {
            if (_isShutdown) return;
            _isShutdown = true;
            _contextEngine.Dispose();
            AIRequestQueue.Instance?.CancelAllRequests();
            Player2Client.StopHealthCheck();
        }

        internal static void ResetForNewGame()
        {
            _contextEngine.ResetCaches();
            _providerRegistry.Reset();
            _skipCheckRegistry.Reset();
            _incidentRegistry.Reset();
            _settingsTabs.Clear();
            _toggleBehaviors.Clear();
            _modCooldownGetters.Clear();
            _dialogueTriggerFn = null;
            _agentIdentityProvider = null;
            _agentActionBridge = null;
            _audioPlayer = new NullAudioPlayer();
            _parameterTuners.Clear();
            _sensorProviders.Clear();
            _callbackCounter = 0;
            _isShutdown = false;
        }

        internal static HistoryManager GetHistoryManager() => _historyManager;
        public static IContextEngine GetContextEngine() => _contextEngine;
        internal static BudgetScheduler GetContextScheduler() => _contextEngine.GetScheduler();
        internal static EmbeddingSnapshotStore GetEmbeddingSnapshotStore() => _contextEngine.GetEmbeddingSnapshotStore();
        public static FlywheelTelemetryCollector Telemetry { get; } = new FlywheelTelemetryCollector();

        public static void RequestImmediate(AIRequest request, Action<AIResponse> onComplete)
        {
            var queue = AIRequestQueue.Instance;
            if (queue == null)
            {
                Log.Error("[RimMind-Core] AIRequestQueue not initialized.");
                return;
            }
            var client = GetClient();
            if (client == null)
            {
                onComplete?.Invoke(AIResponse.Failure(request.RequestId, "AI client not configured."));
                return;
            }
            queue.EnqueueImmediate(request, onComplete, client);
        }

        public static void PauseQueue() => AIRequestQueue.Instance?.PauseQueue();
        public static void ResumeQueue() => AIRequestQueue.Instance?.ResumeQueue();
        public static int ActiveRequestCount => AIRequestQueue.Instance?.ActiveRequestCount ?? 0;

        public static IReadOnlyList<AIRequestQueue.TrackedRequest> GetActiveRequests()
            => AIRequestQueue.Instance?.GetActiveRequests() ?? new List<AIRequestQueue.TrackedRequest>();

        public static IReadOnlyList<AIRequestQueue.TrackedRequest> GetAllQueuedRequests()
            => AIRequestQueue.Instance?.GetAllQueuedRequests() ?? new List<AIRequestQueue.TrackedRequest>();

        public static int TotalQueuedCount => AIRequestQueue.Instance?.TotalQueuedCount ?? 0;

        public static async Task<NpcChatResult> Chat(ContextRequest request, CancellationToken ct = default)
        {
            if (_isShutdown) return new NpcChatResult { Error = "RimMind is shut down." };
            try
            {
                var driver = StorageDriverFactory.GetDriver();

                if (!driver.IsNpcAlive(request.NpcId) && request.NpcId.StartsWith("NPC-"))
                {
                    if (int.TryParse(request.NpcId.Substring(4), out int thingId))
                    {
                        var pawn = NpcManager.FindPawnByNpcId(request.NpcId);
                        if (pawn != null)
                        {
                            var profile = NpcProfileBuilder.BuildPawnNpc(pawn);
                            await driver.SpawnNpcAsync(profile);
                            var capturedProfile = profile;
                            LongEventHandler.ExecuteWhenFinished(() =>
                                NpcManager.Instance?.SpawnNpc(capturedProfile));
                        }
                    }
                }

                var snapshot = _contextEngine.BuildSnapshot(request);
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

            var queue = AIRequestQueue.Instance;
            var client = GetClient();
            if (queue == null || client == null)
            {
                onComplete?.Invoke(AIResponse.Failure(request.RequestId, "AI client or queue not available."));
                return;
            }

            queue.Enqueue(request, onComplete, client);
        }

        public static ContextSnapshot BuildContextSnapshot(ContextRequest request)
            => _contextEngine.BuildSnapshot(request);

        public static void RequestStructured(ContextRequest request, string schema,
            Action<AIResponse> onComplete, List<StructuredTool>? tools = null)
        {
            if (_isShutdown)
            {
                onComplete?.Invoke(AIResponse.Failure($"Structured_{request.NpcId}", "RimMind is shut down."));
                return;
            }
            var snapshot = _contextEngine.BuildSnapshot(request);
            var aiRequest = new AIRequest
            {
                SystemPrompt = string.Empty,
                Messages = new List<ChatMessage>(snapshot.Messages),
                MaxTokens = snapshot.MaxTokens,
                Temperature = snapshot.Temperature,
                RequestId = $"Structured_{request.NpcId}",
                ModId = request.Scenario.ToString(),
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true,
                Priority = AIRequestPriority.Normal,
            };

            Action<AIResponse> wrappedOnComplete = (response) =>
            {
                try
                {
                    bool parseSuccess = response.Success && !string.IsNullOrEmpty(response.Content);
                    Telemetry.Record(new TelemetryRecord
                    {
                        NpcId = request.NpcId,
                        Scenario = request.Scenario,
                        PromptTokens = response.PromptTokens,
                        CompletionTokens = response.CompletionTokens,
                        TotalTokens = response.TokensUsed,
                        CachedTokens = response.CachedTokens,
                        BudgetValue = snapshot.BudgetValue,
                        KeysIncluded = snapshot.IncludedKeys,
                        KeysTrimmed = snapshot.TrimmedKeys,
                        LayerTokenBreakdown = new Dictionary<string, int>
                        {
                            { "L0", snapshot.Meta.L0Tokens },
                            { "L1", snapshot.Meta.L1Tokens },
                            { "L2", snapshot.Meta.L2Tokens },
                            { "L3", snapshot.Meta.L3Tokens },
                            { "L4", snapshot.Meta.L4Tokens },
                            { "L5", snapshot.Meta.L5Tokens },
                        },
                        KeyChangeFreq = snapshot.KeyChangeCounts.Count > 0
                            ? new Dictionary<string, int>(snapshot.KeyChangeCounts) : null,
                        CacheHitRate = null,
                        ScoreDistribution = snapshot.KeyScores.Count > 0
                            ? new Dictionary<string, float>(snapshot.KeyScores) : null,
                        DiffCount = snapshot.DiffCount,
                        LatencyByLayerMs = snapshot.LatencyByLayerMs.Count > 0
                            ? new Dictionary<string, long>(snapshot.LatencyByLayerMs) : null,
                        RequestLatencyMs = snapshot.BuildStartTicks > 0
                            ? (DateTime.Now.Ticks - snapshot.BuildStartTicks) / TimeSpan.TicksPerMillisecond : 0,
                        ResponseParseSuccess = parseSuccess,
                        TimestampTicks = DateTime.Now.Ticks,
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
                    MaxTokens = snapshot.MaxTokens,
                    Temperature = snapshot.Temperature,
                    RequestId = aiRequest.RequestId,
                    ModId = aiRequest.ModId,
                    ExpireAtTicks = aiRequest.ExpireAtTicks,
                    UseJsonMode = true,
                    Priority = aiRequest.Priority,
                };
                var queue = AIRequestQueue.Instance;
                var client = GetClient();
                if (queue != null && client != null)
                    queue.Enqueue(fallbackRequest, wrappedOnComplete, client);
                else
                    wrappedOnComplete?.Invoke(AIResponse.Failure(fallbackRequest.RequestId, "No AI client available"));
            }
        }

        public static string BuildMapContext(Map map, bool brief = false)
            => GameContextBuilder.BuildMapContext(map, brief);

        public static bool IsConfigured() => RimMindCoreMod.Settings.IsConfigured();

        [Obsolete("Use ContextKeyRegistry.RegisterPawnContext instead")]
        public static void RegisterPawnContextProvider(string category, Func<Pawn, string?> provider,
            int priority = PromptSection.PriorityAuxiliary, string modId = "", bool overrideExisting = true)
            => _providerRegistry.RegisterPawnProvider(category, modId, provider, priority, overrideExisting);

        public static string? GetProviderData(string category, Pawn pawn)
            => _providerRegistry.GetProviderData(category, pawn);

        public static string? GetStaticProviderData(string category)
            => _providerRegistry.GetStaticProviderData(category);

        public static List<string> GetRegisteredCategories()
            => _providerRegistry.GetRegisteredCategories();

        public static void RegisterSettingsTab(string tabId, Func<string> labelFn, Action<UnityEngine.Rect> drawFn)
            => _settingsTabs[tabId] = (tabId, labelFn, drawFn);

        public static IReadOnlyList<(string tabId, Func<string> labelFn, Action<UnityEngine.Rect> drawFn)>
            SettingsTabs => _settingsTabs.Values.ToList();

        public static void RegisterToggleBehavior(string id, Func<bool> isActive, Action toggle)
            => _toggleBehaviors[id] = (id, isActive, toggle);

        public static bool IsAnyToggleActive()
            => _toggleBehaviors.Values.Any(b => b.isActive());

        public static void ToggleAll()
        {
            foreach (var (_, _, toggle) in _toggleBehaviors.Values.ToList())
                toggle();
        }

        public static bool HasToggleBehaviors => _toggleBehaviors.Count > 0;

        public static void RegisterModCooldown(string modId, Func<int> getCooldownTicks)
            => _modCooldownGetters[modId] = getCooldownTicks;

        public static Func<int>? GetModCooldownGetter(string modId)
            => _modCooldownGetters.TryGetValue(modId, out var getter) ? getter : null;

        public static IReadOnlyDictionary<string, Func<int>> ModCooldownGetters => _modCooldownGetters;

        public static void RegisterDialogueTrigger(Action<Pawn, string, Pawn?> triggerFn)
            => _dialogueTriggerFn = triggerFn;

        public static bool CanTriggerDialogue => _dialogueTriggerFn != null;

        public static void TriggerDialogue(Pawn pawn, string context, Pawn? recipient = null)
        {
            if (_dialogueTriggerFn == null)
            {
                Log.Warning("[RimMind-Core] TriggerDialogue called but no dialogue trigger registered.");
                return;
            }
            _dialogueTriggerFn(pawn, context, recipient);
        }

        public static void RegisterDialogueSkipCheck(string sourceId, Func<Pawn, string, bool> skipCheck)
            => _skipCheckRegistry.RegisterDialogueSkipCheck(sourceId, skipCheck);

        public static void UnregisterDialogueSkipCheck(string sourceId)
            => _skipCheckRegistry.UnregisterDialogueSkipCheck(sourceId);

        public static bool ShouldSkipDialogue(Pawn pawn, string triggerType)
            => _skipCheckRegistry.ShouldSkipDialogue(pawn, triggerType);

        public static void RegisterFloatMenuSkipCheck(string sourceId, Func<bool> skipCheck)
            => _skipCheckRegistry.RegisterFloatMenuSkipCheck(sourceId, skipCheck);

        public static void UnregisterFloatMenuSkipCheck(string sourceId)
            => _skipCheckRegistry.UnregisterFloatMenuSkipCheck(sourceId);

        public static bool ShouldSkipFloatMenu()
            => _skipCheckRegistry.ShouldSkipFloatMenu();

        public static void RegisterActionSkipCheck(string sourceId, Func<string, bool> skipCheck)
            => _skipCheckRegistry.RegisterActionSkipCheck(sourceId, skipCheck);

        public static void UnregisterActionSkipCheck(string sourceId)
            => _skipCheckRegistry.UnregisterActionSkipCheck(sourceId);

        public static bool ShouldSkipAction(string intentId)
            => _skipCheckRegistry.ShouldSkipAction(intentId);

        public static string RegisterIncidentExecutedCallback(Action callback)
            => _incidentRegistry.RegisterIncidentExecutedCallback(callback);

        public static void NotifyIncidentExecuted()
            => _incidentRegistry.NotifyIncidentExecuted();

        public static void UnregisterIncidentExecutedCallback(string key)
            => _incidentRegistry.UnregisterIncidentExecutedCallback(key);

        public static string RegisterStorytellerIncidentSkipCheck(Func<bool> check)
            => _skipCheckRegistry.RegisterStorytellerIncidentSkipCheck(check, ref _callbackCounter);

        public static void UnregisterStorytellerIncidentSkipCheck(string key)
            => _skipCheckRegistry.UnregisterStorytellerIncidentSkipCheck(key);

        public static bool ShouldSkipStorytellerIncident()
            => _skipCheckRegistry.ShouldSkipStorytellerIncident();

        public static IEventBus GetEventBus() => _eventBus;

        public static void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => _agentIdentityProvider = provider;

        public static AgentIdentity? GetAgentIdentity(Pawn pawn)
            => _agentIdentityProvider?.Invoke(pawn);

        public static void RegisterAgentActionBridge(IAgentActionBridge bridge)
            => _agentActionBridge = bridge;

        public static IAgentActionBridge? GetAgentActionBridge()
            => _agentActionBridge;

        public static IAudioPlayer AudioPlayer => _audioPlayer;

        public static void PublishPerception(int pawnId, string type, string content, float importance = 0.5f)
            => Perception.PerceptionBridge.PublishPerception(pawnId, type, content, importance);

        public static void RegisterPendingRequest(RequestEntry entry)
            => _overlayService.RegisterPendingRequest(entry);

        public static IReadOnlyList<RequestEntry> GetPendingRequests()
            => _overlayService.GetPendingRequests();

        internal static IAIClient? GetClient()
            => _clientManager.GetClient();

        public static void InvalidateClientCache()
            => _clientManager.InvalidateCache();

        public static Player2Client? GetPlayer2Client()
            => _clientManager.GetPlayer2Client();

        public static void RegisterParameterTuner(IParameterTuner tuner)
            => _parameterTuners[tuner.TunerId] = tuner;

        public static IReadOnlyList<IParameterTuner> ParameterTuners => _parameterTuners.Values.ToList();

        public static void RegisterSensorProvider(ISensorProvider provider)
        {
            _sensorProviders[provider.SensorId] = provider;
            RegisterSensorContextKey(provider);
        }

        public static void UnregisterSensorProvider(string sensorId)
        {
            _sensorProviders.TryRemove(sensorId, out _);
            ContextKeyRegistry.Unregister($"sensor_{sensorId}");
        }

        public static IReadOnlyList<ISensorProvider> SensorProviders => _sensorProviders.Values.ToList();

        public static void ClearModCooldown(string modId) => AIRequestQueue.Instance?.ClearCooldown(modId);

        private static void RegisterSensorContextKey(ISensorProvider sensor)
        {
            string key = $"sensor_{sensor.SensorId}";
            var captured = sensor;
            ContextKeyRegistry.Register(key, ContextLayer.L5_Sensor, captured.Priority / 100f,
                pawn =>
                {
                    string? data = captured.Sense(pawn);
                    if (string.IsNullOrEmpty(data))
                        return new List<ContextEntry>();
                    return new List<ContextEntry> { new ContextEntry(data!) };
                }, "Core");
        }

        internal static void SetContextEngine(IContextEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
        }

        internal static void SetEventBus(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));
        }

        internal static void SetClientManager(ClientManager clientManager)
        {
            if (clientManager == null) throw new ArgumentNullException(nameof(clientManager));
        }
    }
}
