using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Core.Agent;
using RimMind.Core.Client;
using RimMind.Core.Client.Player2;
using RimMind.Core.Extensions;
using RimMind.Core.Internal;
using RimMind.Core.Sensor;
using RimMind.Core.Settings;
using RimMind.Kernel.Bus;
using RimMind.Kernel.Context;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Prompt;
using RimMind.Kernel.Queue;

namespace RimMind.Core.Client
{
    public enum AIRequestState { Pending, Queued, Active, Processing, Completed, Failed, Cancelled, TimedOut, Error }
    public enum AIRequestPriority { Low, Normal, High, Critical }
}

namespace RimMind.Core.Client.Player2
{
    public class Player2Client : IAIClient
    {
        public bool IsLocalEndpoint => false;
        public Task<AIResponse> SendAsync(AIRequest request)
            => Task.FromResult(new AIResponse { Success = false, Error = "Stub" });
        public static void StopHealthCheck() { }
        public bool IsConfigured() => false;
    }
}

namespace RimMind.Core.Runtime
{
    internal sealed class RimMindRuntime : IDisposable
    {
        private static RimMindRuntime? _instance;
        private static readonly object _initLock = new object();

        public static RimMindRuntime Instance => _instance
            ?? throw new InvalidOperationException("RimMindRuntime not initialized.");

        public IAgentBus AgentBus { get; set; } = null!;
        public IEventBus EventBus { get; set; } = null!;
        public IContextEngine ContextEngine { get; set; } = null!;
        public IHistoryManager HistoryManager { get; set; } = null!;
        public IClientManager ClientManager { get; set; } = null!;
        public RimMind.Adapters.UI.IAudioPlayer AudioPlayer { get; set; } = null!;
        public IProviderRegistry ProviderRegistry { get; set; } = null!;
        public IOverlayService OverlayService { get; set; } = null!;
        public AIRequestQueueImpl QueueImpl { get; set; } = null!;
        public IAIRequestQueue Queue => QueueImpl;
        public FlywheelTelemetryCollector Telemetry { get; set; } = null!;

        public bool IsShutdown => false;

        private RimMindRuntime()
        {
            ProviderRegistry = new ProviderRegistry();
            ClientManager = new RimMind.Core.Internal.ClientManager();
            OverlayService = new OverlayService();
            HistoryManager = new HistoryManager();
            ContextEngine = new ContextEngine(HistoryManager);
            AgentBus = new AgentBusImpl();
            EventBus = new EventBusAdapter(AgentBus);
            AudioPlayer = new RimMind.Adapters.UI.NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            QueueImpl = new AIRequestQueueImpl();
            RimMindServiceLocator.Register<IHistoryManager>(HistoryManager);
        }

        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_instance != null) return;
                _instance = new RimMindRuntime();
            }
        }

        public static void ResetInstance() { _instance = null; }

        public void Reset()
        {
            RimMindServiceLocator.Reset();
            HistoryManager = new HistoryManager();
            RimMindServiceLocator.Register<IHistoryManager>(HistoryManager);
            ContextEngine = new ContextEngine(HistoryManager);
            ProviderRegistry.Reset();
            AudioPlayer = new RimMind.Adapters.UI.NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            AgentBus = new AgentBusImpl();
            EventBus = new EventBusAdapter(AgentBus);
            ClientManager = new RimMind.Core.Internal.ClientManager();
            QueueImpl = new AIRequestQueueImpl();
        }

        public void Shutdown() { }

        public void Dispose()
        {
            Shutdown();
            ResetInstance();
        }

        public Task<T?> RequestStructuredAsync<T>(Verse.Pawn pawn, string prompt, CancellationToken ct = default) where T : class
            => Task.FromResult<T?>(null);

        public void RequestStructuredAsync(AIRequest request, string schema, Action<AIResponse> onResponse, List<StructuredTool>? tools = null)
        {
        }

        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
            => new RimMind.Kernel.Registry.ExtensionRegistry<T>();

        public void RegisterAgentIdentityProvider(Func<Verse.Pawn, AgentIdentity?> provider) { }
        public AgentIdentity? GetAgentIdentity(Verse.Pawn pawn) => null;
        public void RegisterAgentActionBridge(IAgentActionBridge bridge) { }
        public IAgentActionBridge? GetAgentActionBridge() => null;
        public void RegisterParameterTuner(IParameterTuner tuner) { }
        public void RegisterSensorProvider(ISensorProvider provider) { }
        public void UnregisterSensorProvider(string sensorId) { }
        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public Player2Client? GetPlayer2Client() => ClientManager.GetPlayer2Client();
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => ContextEngine.GetEmbeddingSnapshotStore();

        public List<IParameterTuner> ParameterTunersList => new List<IParameterTuner>();
        public List<ISensorProvider> SensorProvidersList => new List<ISensorProvider>();

        public IDisposable WithOverrides(Action<RimMindRuntime> configure)
        {
            var snapshot = new RuntimeSnapshot(this);
            configure(this);
            return new OverrideScope(this, snapshot);
        }

        private sealed class OverrideScope : IDisposable
        {
            private readonly RimMindRuntime _runtime;
            private readonly RuntimeSnapshot _snapshot;
            private int _disposed;

            public OverrideScope(RimMindRuntime runtime, RuntimeSnapshot snapshot)
            {
                _runtime = runtime;
                _snapshot = snapshot;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _snapshot.Restore(_runtime);
                }
            }
        }

        private sealed class RuntimeSnapshot
        {
            private readonly IEventBus _eventBus;
            private readonly IContextEngine _contextEngine;
            private readonly IHistoryManager _historyManager;
            private readonly IClientManager _clientManager;
            private readonly RimMind.Adapters.UI.IAudioPlayer _audioPlayer;
            private readonly IProviderRegistry _providerRegistry;
            private readonly IOverlayService _overlayService;
            private readonly AIRequestQueueImpl _queueImpl;
            private readonly FlywheelTelemetryCollector _telemetry;
            private readonly IAgentBus _agentBus;

            public RuntimeSnapshot(RimMindRuntime r)
            {
                _eventBus = r.EventBus;
                _contextEngine = r.ContextEngine;
                _historyManager = r.HistoryManager;
                _clientManager = r.ClientManager;
                _audioPlayer = r.AudioPlayer;
                _providerRegistry = r.ProviderRegistry;
                _overlayService = r.OverlayService;
                _queueImpl = r.QueueImpl;
                _telemetry = r.Telemetry;
                _agentBus = r.AgentBus;
            }

            public void Restore(RimMindRuntime r)
            {
                r.EventBus = _eventBus;
                r.ContextEngine = _contextEngine;
                r.HistoryManager = _historyManager;
                r.ClientManager = _clientManager;
                r.AudioPlayer = _audioPlayer;
                r.ProviderRegistry = _providerRegistry;
                r.OverlayService = _overlayService;
                r.QueueImpl = _queueImpl;
                r.Telemetry = _telemetry;
                r.AgentBus = _agentBus;
            }
        }
    }
}

namespace RimMind.Core.Tests
{
    internal sealed class VerseTickProvider : RimMind.Kernel.Abstractions.ITickProvider
    {
        public int TicksGame => Verse.Find.TickManager?.TicksGame ?? 0;
    }
}

namespace RimMind.Core.Sensor
{
    public class SensorManager : Verse.GameComponent, ISensorManager
    {
        public static ISensorManager? Instance => RimMindServiceLocator.Get<ISensorManager>();
        public SensorManager() : base() { }
        public SensorManager(Verse.Game game) : base() { }
        public List<RimMind.Core.Client.StructuredTool> BuildAgentTools(Verse.Pawn pawn) => new();
        public void RegisterSensorContextKeys() { }
    }
}

namespace RimMind.Core.Agent
{
    using RimMind.Core.Client;

    public class PerceptionPipeline
    {
        private readonly List<object> _filters = new List<object>();
        public PerceptionPipeline AddFilter(object filter) { _filters.Add(filter); return this; }
        public List<PerceptionBufferEntry> Process(IReadOnlyList<PerceptionBufferEntry> entries) => new List<PerceptionBufferEntry>(entries);
        public List<string> Process(object perception) => new List<string>();
    }

    public class DedupFilter { }
    public class PriorityFilter { }
    public class CooldownFilter { }
}

namespace RimMind.Adapters.UI
{
    public class RequestEntry
    {
        public string NpcId { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public interface IAudioPlayer
    {
        void PlayAudio(string audioUrl);
    }

    internal sealed class NullAudioPlayer : IAudioPlayer
    {
        public void PlayAudio(string audioUrl) { }
    }

    public class RimMindCoreSettingsUI
    {
        public static void Draw(UnityEngine.Rect rect) { }
    }
}

namespace RimMind.Core.Internal
{
    public class OverlayService : IOverlayService
    {
        private readonly List<RimMind.Adapters.UI.RequestEntry> _entries = new List<RimMind.Adapters.UI.RequestEntry>();
        public void RegisterPendingRequest(RimMind.Adapters.UI.RequestEntry entry) { _entries.Add(entry); }
        public IReadOnlyList<RimMind.Adapters.UI.RequestEntry> GetPendingRequests() => _entries.AsReadOnly();
    }

    public class ClientManager : IClientManager
    {
        public IAIClient? GetClient() => null;
        public void InvalidateCache() { }
        public Player2Client? GetPlayer2Client() => null;
    }
}

namespace RimMind.Core
{
    using RimMind.Core.Settings;
    using RimMind.Core.Extensions;
    using RimMind.Core.Sensor;

    public class RimMindCoreMod : Verse.Mod
    {
        public static RimMindCoreSettings? Settings { get; set; }

        public RimMindCoreMod(Verse.ModContentPack content) : base(content) { }
    }

    public static class RimMindAPI
    {
        public static void RegisterAgentActionBridge(IAgentActionBridge bridge) { }
        public static IAgentActionBridge? GetAgentActionBridge() => null;
        public static void RegisterParameterTuner(IParameterTuner tuner) { }
        public static void RegisterSensorProvider(ISensorProvider provider) { }
        public static void UnregisterSensorProvider(string sensorId) { }
        public static IEventBus GetEventBus() => RimMind.Core.Runtime.RimMindRuntime.Instance.EventBus;
        public static IHistoryManager GetHistoryManager() => RimMind.Core.Runtime.RimMindRuntime.Instance.HistoryManager;
        public static Player2Client? GetPlayer2Client() => RimMind.Core.Runtime.RimMindRuntime.Instance.GetPlayer2Client();
    }
}

namespace RimMind.Core.Npc
{
    public interface INpcManager
    {
        void SpawnNpc(NpcProfile profile);
        void KillNpc(string npcId);
        bool IsNpcAlive(string npcId);
        NpcProfile? GetNpc(string npcId);
        IReadOnlyList<NpcProfile> GetAllNpcs();
        string GetNpcForMap(Verse.Map map);
        Verse.Pawn? FindPawnByNpcId(string npcId);
        Verse.Pawn? FindProxyPawnForMap(Verse.Map map);
        void RegisterActiveAgent(int thingId);
        void UnregisterActiveAgent(int thingId);
        HashSet<int> GetActiveAgentPawnIds();
        void IndexPawn(Verse.Pawn pawn);
        void UnindexPawn(int thingId);
        string GetMapNpcId(Verse.Map map);
    }

    public class NpcManager : Verse.GameComponent, INpcManager
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, Verse.Pawn> _pawnIndex = new();
        public static INpcManager? Instance => RimMindServiceLocator.Get<INpcManager>();

        public NpcManager() : base() { }
        public NpcManager(Verse.Game game) : base() { }

        public void SpawnNpc(NpcProfile profile) { }
        public void KillNpc(string npcId) { }
        public bool IsNpcAlive(string npcId) => false;
        public NpcProfile? GetNpc(string npcId) => null;
        public IReadOnlyList<NpcProfile> GetAllNpcs() => Array.Empty<NpcProfile>();
        public string GetNpcForMap(Verse.Map map) => "";

        public Verse.Pawn? FindPawnByNpcId(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || !npcId.StartsWith("NPC-")) return null;
            if (!int.TryParse(npcId.Substring(4), out int thingId)) return null;

            if (_pawnIndex.TryGetValue(thingId, out var indexed))
            {
                if (!indexed.DestroyedOrNull() && !indexed.Dead) return indexed;
                _pawnIndex.TryRemove(thingId, out _);
            }

            foreach (var map in Verse.Find.Maps)
            {
                if (map?.mapPawns == null) continue;
                var pawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == thingId);
                if (pawn != null)
                {
                    _pawnIndex[thingId] = pawn;
                    return pawn;
                }
            }

            var worldPawn = Verse.Find.WorldPawns?.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == thingId);
            if (worldPawn != null)
            {
                _pawnIndex[thingId] = worldPawn;
                return worldPawn;
            }

            return null;
        }

        public Verse.Pawn? FindProxyPawnForMap(Verse.Map map) => null;
        public void RegisterActiveAgent(int thingId) { }
        public void UnregisterActiveAgent(int thingId) { }
        public HashSet<int> GetActiveAgentPawnIds() => new HashSet<int>();
        public void IndexPawn(Verse.Pawn pawn) { if (pawn != null) _pawnIndex[pawn.thingIDNumber] = pawn; }
        public void UnindexPawn(int thingId) { _pawnIndex.TryRemove(thingId, out _); }
        public string GetMapNpcId(Verse.Map map) => "";
        public void ClearPawnIndex() { _pawnIndex.Clear(); }
    }

    internal static class TransientExceptionChecker
    {
        public static bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is HttpHelper.HttpException httpEx && httpEx.StatusCode >= 500 && httpEx.StatusCode < 600) return true;
            return false;
        }
    }

    public class NpcProfileBuilder
    {
        public static object BuildPawnNpc(Verse.Pawn pawn) => new object();
    }
}

namespace RimMind.Adapters.Verse
{
    public class FlywheelGameComponent : global::Verse.GameComponent
    {
        public FlywheelGameComponent() : base(null!) { }
        public FlywheelGameComponent(global::Verse.Game game) : base(game) { }
    }
}

namespace RimMind.Kernel.Context
{
    public interface IStorageDriver
    {
        bool IsNpcAlive(string npcId);
        Task<object> SpawnNpcAsync(object profile);
        Task<RimMind.Core.Npc.NpcChatResult> ChatAsync(ContextSnapshot? snapshot, CancellationToken ct = default);
    }

    internal sealed class StubStorageDriver : IStorageDriver
    {
        public bool IsNpcAlive(string npcId) => false;
        public Task<object> SpawnNpcAsync(object profile) => Task.FromResult(new object());
        public Task<RimMind.Core.Npc.NpcChatResult> ChatAsync(ContextSnapshot? snapshot, CancellationToken ct = default)
            => Task.FromResult(new RimMind.Core.Npc.NpcChatResult());
    }

    public class PromptBudget
    {
        public int TotalTokens { get; set; }
        public int RemainingTokens => TotalTokens;
        public PromptBudget(int totalTokens, int reserveForOutput = 0) { TotalTokens = totalTokens; }
        public List<PromptSection>? Compose(List<PromptSection> sections) => sections;
    }

    public static class PawnDataExtractor
    {
        public static PawnExtractResult Extract(object pawn) => new PawnExtractResult();
        public static string ExtractAll(Verse.Pawn pawn) => "";
    }

    public class PawnExtractResult
    {
        public string? MoodString;
        public float MoodPercent;
        public bool HasMap;
        public float Temperature;
    }
}

namespace RimMind.Kernel.Flywheel
{
    internal class FlywheelBuiltinTuner : IParameterTuner
    {
        public string TunerId => "builtin";
        public void Tune(BudgetSchedulerConfig config) { }
    }

    internal static class FlywheelAnalysisReportWriter
    {
        public static string Write(AnalysisReportRecord record) => "";
    }
}

namespace RimMind.Core.Client
{
    public static class HttpHelper
    {
        public class HttpException : Exception
        {
            public int StatusCode { get; }
            public HttpException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
        }

        public static Task<(string body, int statusCode)> PostAsync(string url, string jsonBody, string authHeader, int timeoutMs = 30000)
            => Task.FromResult(("", 200));
    }

    public class QuotaExceededException : Exception
    {
        public QuotaExceededException(string message) : base(message) { }
        public static bool IsQuotaError(string? error) => false;
    }
}

namespace RimMind.Core.Internal
{
    public static class GameContextBuilder
    {
        public static string BuildMapContext(Verse.Map map, bool brief = false) => "";
        public static List<ContextEntry> BuildMapContextEntries(Verse.Map map, bool brief = false) => new();
        public static string ExtractNearbyPawns(Verse.Pawn pawn) => "";
        public static string ExtractSeason(Verse.Pawn pawn) => "";
        public static string ExtractColonyStatus(Verse.Pawn pawn) => "";
        public static string ExtractHealth(Verse.Pawn pawn) => "";
        public static string ExtractMood(Verse.Pawn pawn) => "";
        public static string ExtractSkills(Verse.Pawn pawn) => "";
        public static string ExtractSkillsSummary(Verse.Pawn pawn) => "";
        public static string ExtractRelations(Verse.Pawn pawn) => "";
        public static string ExtractFixedRelations(Verse.Pawn pawn) => "";
        public static string ExtractInventory(Verse.Pawn pawn) => "";
        public static string ExtractRecentMemories(Verse.Pawn pawn) => "";
        public static string ExtractCurrentJob(Verse.Pawn pawn) => "";
        public static string ExtractThoughts(Verse.Pawn pawn) => "";
        public static string ExtractPosition(Verse.Pawn pawn) => "";
        public static string ExtractFaction(Verse.Pawn pawn) => "";
        public static string ExtractEquipment(Verse.Pawn pawn) => "";
        public static string ExtractPawnBaseInfo(Verse.Pawn pawn) => "";
        public static string ExtractIdeology(Verse.Pawn pawn) => "";
        public static string ExtractCurrentArea(Verse.Pawn pawn) => "";
        public static string ExtractWeather(Verse.Pawn pawn) => "";
        public static string ExtractTimeOfDay(Verse.Pawn pawn) => "";
        public static string ExtractCombatStatus(Verse.Pawn pawn) => "";
        public static string ExtractTargetInfo(Verse.Pawn pawn) => "";
    }
}

namespace RimMind.Kernel.Prompt
{
    public static class GoalGenerator
    {
        public static List<RimMind.Core.Agent.AgentGoal> GenerateFromIdentity(Verse.Pawn pawn) => new();
        public static List<RimMind.Core.Agent.AgentGoal> GenerateFromState(Verse.Pawn pawn) => new();
        public static List<RimMind.Core.Agent.AgentGoal> GenerateFromEvent(string perceptionType, string content) => new();
    }
}

namespace RimMind.Core.Agent
{
    public static class Context
    {
        public static string Build(Verse.Pawn pawn) => "";
        public static SchemaRegistry SchemaRegistry => new SchemaRegistry();
    }

    public class SchemaRegistry
    {
        public string AgentDecision => "AgentDecision";
    }
}
