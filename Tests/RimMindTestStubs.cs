using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Domain.Events;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Runtime;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Agent;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Presentation.Runtime;
using RimMind.Application.Features.Registry;
using RimMind.Presentation.Sensor;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Prompt;
using RimMind.Application.Features.Queue;
using RimMind.Presentation.Settings;

using IParameterTunerContract = RimMind.Application.Common.Interfaces.Extension.IParameterTuner;
using IAgentActionBridgeContract = RimMind.Application.Common.Interfaces.Extension.IAgentActionBridge;
using IStorageDriverApp = RimMind.Application.Common.Interfaces.Npc.IStorageDriver;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public class Player2Client : IAIClient
    {
        public bool IsLocalEndpoint => false;
        public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
            => Task.FromResult(Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("Stub")));
        public Task<Result<AIResponse, RimMindError>> SendStructuredAsync(AIRequest request, string? jsonSchema, List<StructuredTool>? tools)
            => Task.FromResult(Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("Stub")));
        public static void StopHealthCheck() { }
        public bool IsConfigured() => false;
    }
}

namespace RimMind.Presentation.Runtime
{
    internal sealed class RimMindRuntime : IDisposable, IRimMindRuntime
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
        public RimMind.Infrastructure.UI.IAudioPlayer AudioPlayer { get; set; } = null!;
        public IProviderRegistry ProviderRegistry { get; set; } = null!;
        public IOverlayService OverlayService { get; set; } = null!;
        public AIRequestQueueImpl QueueImpl { get; set; } = null!;
        public IAIRequestQueue Queue => QueueImpl;
        public FlywheelTelemetryCollector Telemetry { get; set; } = null!;
        public RimMind.Application.Features.Tools.ToolRegistry ToolRegistry { get; set; } = new();
        public RimMind.Infrastructure.Mechanisms.GameMechanismRegistry MechanismRegistry { get; set; } = new();

        public bool IsShutdown { get; set; }

        private RimMindRuntime()
        {
            ProviderRegistry = new ProviderRegistry();
            ClientManager = new RimMind.Presentation.Runtime.ClientManager();
            OverlayService = new RimMind.Presentation.Runtime.OverlayService();
            HistoryManager = new HistoryManager();
            ContextEngine = new ContextEngine(HistoryManager);
            AgentBus = new AgentBusImpl();
            EventBus = new EventBusAdapter(AgentBus);
            AudioPlayer = new RimMind.Infrastructure.UI.NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            QueueImpl = new AIRequestQueueImpl();
            IsShutdown = false;
            RimMindServiceLocator.Register<IHistoryManager>(HistoryManager);
            RimMindServiceLocator.Register<IRimMindRuntime>(this);
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
            AudioPlayer = new RimMind.Infrastructure.UI.NullAudioPlayer();
            Telemetry = new FlywheelTelemetryCollector();
            AgentBus = new AgentBusImpl();
            EventBus = new EventBusAdapter(AgentBus);
            ClientManager = new RimMind.Presentation.Runtime.ClientManager();
            QueueImpl = new AIRequestQueueImpl();
            ToolRegistry = new RimMind.Application.Features.Tools.ToolRegistry();
            MechanismRegistry = new RimMind.Infrastructure.Mechanisms.GameMechanismRegistry();
        }

        public void Shutdown() { IsShutdown = true; }

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
            => new ExtensionRegistry<T>();

        void IRimMindRuntime.RegisterParameterTuner(IKernelParameterTuner tuner) { }
        IReadOnlyList<IKernelParameterTuner> IRimMindRuntime.ParameterTunersList => new List<IKernelParameterTuner>();
        IExtensionRegistry<T> IRimMindRuntime.GetExtensionRegistry<T>() => new ExtensionRegistry<T>();

        public void RegisterAgentIdentityProvider(Func<Verse.Pawn, AgentIdentity?> provider) { }
        public AgentIdentity? GetAgentIdentity(Verse.Pawn pawn) => null;
        public void RegisterAgentActionBridge(IAgentActionBridgeContract bridge) { }
        public IAgentActionBridgeContract? GetAgentActionBridge() => null;
        public void RegisterParameterTuner(IParameterTunerContract tuner) { }
        public void RegisterSensorProvider(ISensorProvider provider) { }
        public void UnregisterSensorProvider(string sensorId) { }
        public IAIClient? GetClient() => ClientManager.GetClient();
        public void InvalidateClientCache() => ClientManager.InvalidateCache();
        public object? GetPlayer2Client() => ClientManager.GetPlayer2Client();
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => ContextEngine.GetEmbeddingSnapshotStore();

        public List<IParameterTunerContract> ParameterTunersList => new List<IParameterTunerContract>();
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
            private readonly RimMind.Infrastructure.UI.IAudioPlayer _audioPlayer;
            private readonly IProviderRegistry _providerRegistry;
            private readonly IOverlayService _overlayService;
            private readonly AIRequestQueueImpl _queueImpl;
            private readonly FlywheelTelemetryCollector _telemetry;
            private readonly IAgentBus _agentBus;
            private readonly RimMind.Application.Features.Tools.ToolRegistry _toolRegistry;
            private readonly RimMind.Infrastructure.Mechanisms.GameMechanismRegistry _mechanismRegistry;

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
                _toolRegistry = r.ToolRegistry;
                _mechanismRegistry = r.MechanismRegistry;
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
                r.ToolRegistry = _toolRegistry;
                r.MechanismRegistry = _mechanismRegistry;
            }
        }
    }
}

namespace RimMind.Presentation.Runtime
{
    public sealed class EventBusAdapter : IEventBus
    {
        private readonly IAgentBus _bus;
        public EventBusAdapter(IAgentBus bus) => _bus = bus;
        public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent => _bus.Subscribe(key, handler);
        public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent => _bus.Subscribe(handler);
        public void Unsubscribe<T>(string key) where T : AgentBusEvent => _bus.Unsubscribe<T>(key);
        public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent => _bus.Unsubscribe(handler);
        public void Publish<T>(T evt) where T : AgentBusEvent => _bus.Publish(evt);
        public void PublishFromBackground<T>(T evt) where T : AgentBusEvent => _bus.PublishFromBackground(evt);
        public void FlushBackgroundQueue() => _bus.FlushBackgroundQueue();
        public void ClearAllSubscribers() => _bus.ClearAllSubscribers();
        public int GetHandlerCount() => 0;
        public int GetBackgroundQueueCount() => 0;
    }
}

namespace RimMind.Tests
{
    internal sealed class VerseTickProvider : RimMind.Application.Common.Interfaces.Abstractions.ITickProvider
    {
        public int TicksGame => Verse.Find.TickManager?.TicksGame ?? 0;
    }
}

namespace RimMind.Presentation.Sensor
{
    public class SensorManager : Verse.GameComponent, ISensorManager
    {
        public static ISensorManager? Instance => RimMindServiceLocator.Get<ISensorManager>();
        public SensorManager() : base() { }
        public SensorManager(Verse.Game game) : base() { }
        public List<StructuredTool> BuildAgentTools(object pawn) => new();
        public void RegisterSensorContextKeys() { }
    }
}

namespace RimMind.Presentation.Agent
{
    using RimMind.Infrastructure.Services.Clients;

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

namespace RimMind.Infrastructure.UI
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

namespace RimMind.Presentation.Runtime
{
    public class OverlayService : IOverlayService
    {
        private readonly List<RimMind.Application.Common.Models.UI.RequestEntry> _entries = new List<RimMind.Application.Common.Models.UI.RequestEntry>();
        public void RegisterPendingRequest(RimMind.Application.Common.Models.UI.RequestEntry entry) { _entries.Add(entry); }
        public IReadOnlyList<RimMind.Application.Common.Models.UI.RequestEntry> GetPendingRequests() => _entries.AsReadOnly();
    }

    public class ClientManager : IClientManager
    {
        public IAIClient? GetClient() => null;
        public void InvalidateCache() { }
        public object? GetPlayer2Client() => null;
    }
}

namespace RimMind.Presentation
{
    using RimMind.Presentation.Settings;

    public class RimMindCoreMod : Verse.Mod
    {
        public static RimMindCoreSettings? Settings { get; set; }

        public RimMindCoreMod(Verse.ModContentPack content) : base(content) { }
    }

    public static class RimMindAPI
    {
        public static void RegisterAgentActionBridge(IAgentActionBridgeContract bridge) { }
        public static IAgentActionBridgeContract? GetAgentActionBridge() => null;
        public static void RegisterParameterTuner(IParameterTunerContract tuner) { }
        public static void RegisterSensorProvider(ISensorProvider provider) { }
        public static void UnregisterSensorProvider(string sensorId) { }
        public static IEventBus GetEventBus() => RimMind.Presentation.Runtime.RimMindRuntime.Instance.EventBus;
        public static IHistoryManager GetHistoryManager() => RimMind.Presentation.Runtime.RimMindRuntime.Instance.HistoryManager;
        public static object? GetPlayer2Client() => RimMind.Presentation.Runtime.RimMindRuntime.Instance.GetPlayer2Client();
        public static RimMind.Application.Common.Interfaces.Tools.IToolRegistry Tools => RimMind.Presentation.Runtime.RimMindRuntime.Instance.ToolRegistry;
        public static RimMind.Application.Common.Interfaces.Mechanisms.IGameMechanismRegistry Mechanisms => RimMind.Presentation.Runtime.RimMindRuntime.Instance.MechanismRegistry;
    }
}

namespace RimMind.Infrastructure.Verse
{
    using RimMind.Infrastructure.Services.Clients;
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

        string INpcManager.GetNpcForMap(object map) => GetNpcForMap((Verse.Map)map!);
        object? INpcManager.FindPawnByNpcId(string npcId) => FindPawnByNpcId(npcId);
        object? INpcManager.FindProxyPawnForMap(object map) => FindProxyPawnForMap((Verse.Map)map!);
        void INpcManager.IndexPawn(object pawn) => IndexPawn((Verse.Pawn)pawn!);
        string INpcManager.GetMapNpcId(object map) => GetMapNpcId((Verse.Map)map!);
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
        public static NpcProfile BuildPawnNpc(Verse.Pawn pawn) => new NpcProfile();
    }

    public static class StorageDriverFactory
    {
        private static IStorageDriverApp _driver = new StubStorageDriver();
        public static IStorageDriverApp GetDriver() => _driver;
        public static void InvalidateCache() { }
    }
}

namespace RimMind.Infrastructure.Verse
{
    public class FlywheelGameComponent : global::Verse.GameComponent
    {
        public FlywheelGameComponent() : base(null!) { }
        public FlywheelGameComponent(global::Verse.Game game) : base(game) { }
    }
}

namespace RimMind.Application.Features.Context
{
    internal sealed class StubStorageDriver : IStorageDriverApp
    {
        public bool IsRemote => false;
        public bool SupportsStreaming => false;
        public bool SupportsTts => false;
        public bool SupportsCommands => false;
        public bool SupportsStructuredOutput => false;
        public bool IsNpcAlive(string npcId) => false;
        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public async IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string senderName, string query, Action<string>? onChunk, string? gameStateInfo = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return Result<NpcChatChunk, RimMindError>.Ok(new NpcChatChunk(npcId, "", isFinal: true));
            await Task.CompletedTask;
        }
        public Task<Result<bool, RimMindError>> PutAsync(string key, string value) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<string?, RimMindError>> GetAsync(string key) => Task.FromResult(Result<string?, RimMindError>.Ok((string?)null));
        public Task<Result<bool, RimMindError>> DeleteAsync(string key) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<Dictionary<string, string>, RimMindError>> GetBatchAsync(IEnumerable<string> keys) => Task.FromResult(Result<Dictionary<string, string>, RimMindError>.Ok(new Dictionary<string, string>()));
        public Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<string?, RimMindError>> LoadAllEntriesAsync() => Task.FromResult(Result<string?, RimMindError>.Ok((string?)null));
        public Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10) => Task.FromResult(Result<List<string>, RimMindError>.Ok(new List<string>()));
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

namespace RimMind.Application.Features.Flywheel
{
    internal class FlywheelBuiltinTuner : IParameterTunerContract
    {
        public string Name => "builtin";
        public string TunerId => "builtin";
        public float TuneParameter(string parameterName, float currentValue) => currentValue;
        public bool ShouldApply(string npcId) => true;
        public void Tune(object config) { }
    }

    internal static class FlywheelAnalysisReportWriter
    {
        public static string Write(AnalysisReportRecord record) => "";
    }
}

namespace RimMind.Infrastructure.Services.Clients
{
    public static class HttpHelper
    {
        public class HttpException : Exception
        {
            public int StatusCode { get; }
            public HttpException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
        }

        public static Task<(string body, int statusCode)> PostAsync(string url, string jsonBody, string authHeader, int timeoutMs = 30000, float connectTimeout = 5000f)
            => Task.FromResult(("", 200));
    }

    public class QuotaExceededException : Exception
    {
        public QuotaExceededException(string message) : base(message) { }
        public static bool IsQuotaError(string? error) => false;
    }
}

namespace RimMind.Presentation.Agent
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

namespace RimMind.Application.Features.Prompt
{
    public static class GoalGenerator
    {
        public static List<RimMind.Presentation.Agent.AgentGoal> GenerateFromIdentity(Verse.Pawn pawn) => new();
        public static List<RimMind.Presentation.Agent.AgentGoal> GenerateFromState(Verse.Pawn pawn) => new();
        public static List<RimMind.Presentation.Agent.AgentGoal> GenerateFromEvent(string perceptionType, string content) => new();
    }
}

namespace RimMind.Presentation.Agent
{
    public static class Context
    {
        public static string Build(Verse.Pawn pawn) => "";
        public static SchemaRegistry SchemaRegistry => new SchemaRegistry();
    }

    public class SchemaRegistry
    {
        public static string AgentDecision => "AgentDecision";
    }
}
