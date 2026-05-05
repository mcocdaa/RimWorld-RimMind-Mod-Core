using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
    }
}

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public static class StringExtensions
    {
        public static TaggedString Translate(this string key) => new TaggedString { Value = key };
        public static TaggedString Translate(this string key, object arg0) => new TaggedString { Value = string.Format(key, arg0) };
        public static TaggedString Translate(this string key, object arg0, object arg1) => new TaggedString { Value = string.Format(key, arg0, arg1) };
        public static TaggedString Translate(this string key, object arg0, object arg1, object arg2) => new TaggedString { Value = string.Format(key, arg0, arg1, arg2) };
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public class GameComponent : IExposable
    {
        public GameComponent() { }
        public GameComponent(Game game) { }
        public virtual void ExposeData() { }
        public virtual void FinalizeInit() { }
        public virtual void StartedNewGame() { }
        public virtual void LoadedGame() { }
        public virtual void GameComponentTick() { }
    }

    public class Game { }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T defaultValue = default) { }
    }

    public static class Scribe_Collections
    {
        public static void Look<T>(ref List<T> list, string label, LookMode lookMode) { }
        public static void Look<T>(ref List<T> list, string label) { }
        public static void Look<TKey, TValue>(ref Dictionary<TKey, TValue> dict, string label, LookMode keyLookMode, LookMode valueLookMode) where TKey : notnull { }
    }

    public enum LookMode { Value, Deep }

    public static class Scribe_Deep
    {
        public static void Look<T>(ref T? value, string label) where T : IExposable, new() { value ??= new T(); }
    }

    public enum LoadSaveMode { Saving, LoadingVars, PostLoadInit }

    public static class Scribe
    {
        public static LoadSaveMode mode = LoadSaveMode.Saving;
    }

    public static class Log
    {
        public static Action<string> Warning = _ => { };
        public static Action<string> Message = _ => { };
        public static Action<string> Error = _ => { };
    }

    public class Thing
    {
        public int thingIDNumber;
    }

    public class ThingWithComps : Thing
    {
        private List<ThingComp> _comps = new List<ThingComp>();

        public T? GetComp<T>() where T : ThingComp
        {
            foreach (var comp in _comps)
                if (comp is T t) return t;
            return null;
        }

        public T TryGetComp<T>() where T : ThingComp => GetComp<T>()!;

        internal void AddComp(ThingComp comp)
        {
            comp.parent = this;
            _comps.Add(comp);
        }
    }

    public class ThingComp
    {
        public ThingWithComps? parent;
    }

    public class CompProperties
    {
        public Type? compClass;
    }

    public class Need
    {
        public float CurLevelPercentage = 1f;
    }

    public class Pawn_NeedsTracker
    {
        public Need? mood;
        public Need? food;
    }

    public class Pawn_HealthTracker
    {
        public bool HasHediffsNeedingTend() => false;
    }

    public class Name
    {
        public string ToStringShort = "";
    }

    public class Pawn : ThingWithComps
    {
        public bool Dead;
        public bool IsFreeNonSlaveColonist;
        public bool DestroyedOrNull() => false;
        public Map? Map;
        public Pawn_NeedsTracker? needs;
        public Pawn_HealthTracker? health;
        public string LabelShortCap = "";
        public Name? Name;

        public bool IsHashIntervalTick(int interval)
        {
            return thingIDNumber % interval == 0;
        }
    }

    public class MapPawns
    {
        public List<Pawn> AllPawns = new List<Pawn>();
        public List<Pawn> FreeColonists = new List<Pawn>();
        public List<Pawn> FreeColonistsAndPrisoners = new List<Pawn>();
    }

    public class Map
    {
        public int uniqueID;
        public MapPawns? mapPawns;
    }

    public class WorldPawns
    {
        public List<Pawn> AllPawnsAlive = new List<Pawn>();
    }

    public static class Find
    {
        public static TickManager TickManager = new TickManager();
        public static List<Map> Maps = new List<Map>();
        public static WorldPawns? WorldPawns;
    }

    public class TickManager
    {
        public int TicksGame = 100000;
        public bool Paused = false;
    }

    public static class GenFilePaths
    {
        public static string SaveDataFolderPath = "/tmp/test";
    }

    public static class LongEventHandler
    {
        public static void ExecuteWhenFinished(Action action) { action(); }
    }
}

namespace Verse.AI
{
    public class Job { }

    public struct JobIssueParams { }

    public class ThinkNode
    {
        public float priority = 0f;
        public virtual ThinkResult TryIssueJobPackage(Verse.Pawn pawn, JobIssueParams jobParams) => default;
        public virtual float GetPriority(Verse.Pawn pawn) => 0f;
        public virtual ThinkNode DeepCopy(bool resolve = true) => (ThinkNode)MemberwiseClone();
    }

    public struct ThinkResult
    {
        public Job? Job;
        public ThinkNode? SourceNode;
        public bool FromQueue;

        public ThinkResult(Job job, ThinkNode sourceNode, Verse.TaggedString? tag, bool fromQueue)
        {
            Job = job;
            SourceNode = sourceNode;
            FromQueue = fromQueue;
        }

        public static ThinkResult NoJob => default;
    }
}

namespace RimWorld
{
}

namespace RimMind.Core
{
    public enum AIProvider { OpenAI, Player2 }

    public class ContextSettings
    {
        public float BudgetW1 = 0.4f;
        public float BudgetW2 = 0.6f;
        public float ContextBudget = 0.6f;
        public int maxCacheEntries = 100;
        public int contextBriefLimit = 200;
        public float moodDiffThreshold = 5f;
        public float temperatureDiffThreshold = 5f;
        public int environmentScanRadius = 5;
        public int environmentMaxItems = 8;
        public float threatThresholdHigh = 200000f;
        public float threatThresholdMedium = 100000f;
        public float threatThresholdLow = 50000f;
    }

    public class AICoreSettings
    {
        public ContextSettings? Context;
        public bool debugLogging;
        public string? telemetryDataPath;
        public string? analysisReportPath;
        public Flywheel.FlywheelAutoApplyMode autoApplyMode = Flywheel.FlywheelAutoApplyMode.Off;
        public float autoApplyConfidenceThreshold = 0.8f;
        public int maxTokens = 800;
        public float defaultTemperature = 0.7f;
        public int thinkCooldownTicks = 30000;
        public int agentTickInterval = 150;
        public int maxToolCallDepth = 3;
        public int requestExpireTicks = 30000;
        public int behaviorHistoryMax = 100;
        public int queueProcessInterval = 60;
        public int defaultModCooldownTicks = 3600;
        public int maxRetryCount = 2;
        public int maxConcurrentRequests = 3;
        public int requestTimeoutMs = 120000;
        public int contextDiffLifetimeTicks = 36000;
        public int contextCalibrateInterval = 10000;
        public AIProvider provider;
        public string? apiKey;
        public string? apiEndpoint;
        public string? modelName;
        public bool forceJsonMode;
        public string? player2RemoteUrl;

        public bool IsConfigured() => !string.IsNullOrEmpty(apiKey);

        public void Validate()
        {
            if (maxTokens < 100) maxTokens = 100;
            defaultTemperature = Math.Clamp(defaultTemperature, 0.0f, 2.0f);
            if (maxConcurrentRequests < 1) maxConcurrentRequests = 1;
            if (requestTimeoutMs < 1000) requestTimeoutMs = 1000;
            if (thinkCooldownTicks < 60) thinkCooldownTicks = 60;
            if (agentTickInterval < 10) agentTickInterval = 10;
            if (maxToolCallDepth < 1) maxToolCallDepth = 1;
            if (contextDiffLifetimeTicks < 600) contextDiffLifetimeTicks = 600;
        }
    }

    public static class RimMindCoreMod
    {
        public static AICoreSettings? Settings;
    }

    public static class RimMindAPI
    {
        private static readonly Context.ContextEngine _contextEngine = new Context.ContextEngine(new Context.HistoryManager());
        public static Flywheel.FlywheelTelemetryCollector Telemetry { get; } = new Flywheel.FlywheelTelemetryCollector();
        public static List<Extensions.IParameterTuner> ParameterTuners = new List<Extensions.IParameterTuner>();
        public static void RegisterParameterTuner(Extensions.IParameterTuner tuner) { }
        internal static void ResetForNewGame() { }
        public static Func<int>? GetModCooldownGetter(string modId) => null;
        public static Context.ContextSnapshot BuildContextSnapshot(Context.ContextRequest request) => new Context.ContextSnapshot();
        public static Agent.IAgentActionBridge? GetAgentActionBridge() => null;
        public static void RequestStructuredAsync(Client.AIRequest request, string? jsonSchema, Action<Client.AIResponse> onComplete, List<Client.StructuredTool>? tools = null) { }
        public static Agent.AgentIdentity? GetAgentIdentity(Verse.Pawn pawn) => null;
        public static List<Extensions.ISensorProvider> SensorProviders = new List<Extensions.ISensorProvider>();
        public static Context.IContextEngine GetContextEngine() => _contextEngine;
        public static Context.EmbeddingSnapshotStore GetEmbeddingSnapshotStore() => new Context.EmbeddingSnapshotStore();
        public static Client.IAIClient? GetClient() => null;
    }
}

namespace RimMind.Core.Settings
{
}

namespace RimMind.Core.Agent
{
    public enum AgentState
    {
        Dormant,
        Active,
        Paused,
        Terminated
    }

    public static class AgentStateTransition
    {
        private static readonly Dictionary<AgentState, HashSet<AgentState>> _allowed = new Dictionary<AgentState, HashSet<AgentState>>
        {
            [AgentState.Dormant] = new HashSet<AgentState> { AgentState.Active, AgentState.Terminated },
            [AgentState.Active] = new HashSet<AgentState> { AgentState.Paused, AgentState.Dormant, AgentState.Terminated },
            [AgentState.Paused] = new HashSet<AgentState> { AgentState.Active, AgentState.Dormant, AgentState.Terminated },
            [AgentState.Terminated] = new HashSet<AgentState>(),
        };

        public static bool CanTransition(AgentState from, AgentState to)
        {
            if (from == to) return false;
            return _allowed.TryGetValue(from, out var targets) && targets.Contains(to);
        }
    }

    public enum GoalStatus
    {
        Proposed,
        Active,
        Achieved,
        Expired,
        Abandoned
    }

    public enum GoalCategory
    {
        Survival,
        Social,
        Work,
        Combat,
        Other
    }

    public class AgentGoal : Verse.IExposable
    {
        public string Description = "";
        public GoalStatus Status = GoalStatus.Proposed;
        public float Priority = 0.5f;
        public GoalCategory Category = GoalCategory.Other;
        public float Progress;
        public int ExpirationTick;

        public AgentGoal() { }

        public AgentGoal(string description, GoalCategory category, float priority, GoalStatus status)
        {
            Description = description;
            Category = category;
            Priority = priority;
            Status = status;
        }

        public bool IsExpired => ExpirationTick > 0 && (Verse.Find.TickManager?.TicksGame ?? 0) >= ExpirationTick;

        public void ExposeData()
        {
            Verse.Scribe_Values.Look(ref Description, "description");
            Verse.Scribe_Values.Look(ref Status, "status", GoalStatus.Proposed);
            Verse.Scribe_Values.Look(ref Priority, "priority", 0.5f);
            Verse.Scribe_Values.Look(ref Category, "category", GoalCategory.Other);
            Verse.Scribe_Values.Look(ref Progress, "progress");
            Verse.Scribe_Values.Look(ref ExpirationTick, "expirationTick");
        }
    }

    public class AgentIdentity : Verse.IExposable
    {
        public string Name = "";
        public string ShortName = "";
        public string CharacterDescription = "";
        public string SystemPrompt = "";
        public List<Npc.NpcCommand> Commands = new List<Npc.NpcCommand>();

        public void ExposeData()
        {
            Verse.Scribe_Values.Look(ref Name, "name");
            Verse.Scribe_Values.Look(ref ShortName, "shortName");
            Verse.Scribe_Values.Look(ref CharacterDescription, "characterDescription");
            Verse.Scribe_Values.Look(ref SystemPrompt, "systemPrompt");
        }
    }

    public class BehaviorRecord : Verse.IExposable
    {
        public string Action = "";
        public string Reason = "";
        public bool Success;
        public string ResultReason = "";
        public float GoalProgressDelta;
        public int Timestamp;
        public string ActionEventId = "";

        public void ExposeData()
        {
            Verse.Scribe_Values.Look(ref Action, "action");
            Verse.Scribe_Values.Look(ref Reason, "reason");
            Verse.Scribe_Values.Look(ref Success, "success");
            Verse.Scribe_Values.Look(ref ResultReason, "resultReason");
            Verse.Scribe_Values.Look(ref GoalProgressDelta, "goalProgressDelta");
            Verse.Scribe_Values.Look(ref Timestamp, "timestamp");
            Verse.Scribe_Values.Look(ref ActionEventId, "actionEventId");
        }
    }

    public class PerceptionBufferEntry
    {
        public string PerceptionType = "";
        public string Content = "";
        public float Importance;
        public int Timestamp;
        public int PawnId;
        public string DedupKey => $"{PerceptionType}:{Content}";
    }

    public class PerceptionBuffer
    {
        private readonly int _capacity;
        private readonly List<PerceptionBufferEntry> _entries = new List<PerceptionBufferEntry>();

        public PerceptionBuffer(int capacity = 20) { _capacity = capacity; }
        public int Capacity => _capacity;

        public IReadOnlyList<PerceptionBufferEntry> Entries => _entries.ToList();

        public void Add(PerceptionBufferEntry entry)
        {
            _entries.Add(entry);
            while (_entries.Count > _capacity)
                _entries.RemoveAt(0);
        }

        public List<PerceptionBufferEntry> Flush()
        {
            var result = new List<PerceptionBufferEntry>(_entries);
            _entries.Clear();
            return result;
        }

        public void Clear() => _entries.Clear();
    }

    public interface IPerceptionFilter
    {
        List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries);
    }

    public class PerceptionPipeline
    {
        private readonly List<IPerceptionFilter> _filters = new List<IPerceptionFilter>();

        public void AddFilter(IPerceptionFilter filter) { _filters.Add(filter); }

        public List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries)
        {
            var current = entries;
            foreach (var filter in _filters)
                current = filter.Process(current);
            return current;
        }
    }

    public class DedupFilter : IPerceptionFilter
    {
        public List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries)
        {
            var seen = new HashSet<string>();
            var result = new List<PerceptionBufferEntry>();
            foreach (var e in entries)
            {
                if (seen.Add(e.DedupKey))
                    result.Add(e);
            }
            return result;
        }
    }

    public class PriorityFilter : IPerceptionFilter
    {
        private readonly float _threshold;

        public PriorityFilter(float threshold = 0.2f) { _threshold = threshold; }

        public List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries)
        {
            return entries.Where(e => e.Importance >= _threshold).ToList();
        }

        public List<PerceptionBufferEntry> Filter(List<PerceptionBufferEntry> entries)
        {
            return Process(entries);
        }
    }

    public class CooldownFilter : IPerceptionFilter
    {
        private readonly Dictionary<string, int> _lastSeen = new Dictionary<string, int>();
        private readonly int _cooldownTicks;

        public CooldownFilter(int cooldownTicks = 600) { _cooldownTicks = cooldownTicks; }

        public List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries)
        {
            var result = new List<PerceptionBufferEntry>();
            foreach (var e in entries)
            {
                if (!_lastSeen.TryGetValue(e.DedupKey, out var last) || e.Timestamp - last >= _cooldownTicks)
                {
                    result.Add(e);
                    _lastSeen[e.DedupKey] = e.Timestamp;
                }
            }
            return result;
        }

        public List<PerceptionBufferEntry> Filter(List<PerceptionBufferEntry> entries)
        {
            return Process(entries);
        }

        public void Reset() { _lastSeen.Clear(); }
    }

    public interface IAgentActionBridge
    {
        bool Execute(string action, Verse.Pawn pawn, Verse.Pawn? target, string? param, string eventId);
        List<Client.StructuredTool>? GetAvailableTools(Verse.Pawn pawn);
    }

    public static class GoalGenerator
    {
        public static List<AgentGoal> GenerateFromIdentity(Verse.Pawn pawn) => new List<AgentGoal>();
        public static List<AgentGoal> GenerateFromState(Verse.Pawn pawn) => new List<AgentGoal>();
        public static List<AgentGoal> GenerateFromEvent(string perceptionType, string content) => new List<AgentGoal>();
    }
}

namespace RimMind.Core.Context
{
    public interface IRelevanceProvider
    {
        float ComputeRelevance(string scenarioId, string npcId, KeyMeta key);
    }

    public enum ContextLayer
    {
        L0_Static,
        L1_Baseline,
        L2_Environment,
        L3_State,
        L5_Sensor
    }

    public class KeyMeta
    {
        public string Key;
        public ContextLayer Layer;
        public float Priority;
        public Func<Verse.Pawn, List<ContextEntry>> ValueProvider;
        public string OwnerMod;
        public bool IsIndexable;
        public float[]? KeyEmbedding;
        public float CurrentScore;
        public float CurrentE;
        public int UpdateCount;
        public float AdaptivePriority;
        public ContextLayer OriginalLayer;

        public KeyMeta(string key, ContextLayer layer, float priority,
            Func<Verse.Pawn, List<ContextEntry>> provider, string ownerMod,
            bool isIndexable = false, float[]? keyEmbedding = null)
        {
            Key = key;
            Layer = layer;
            OriginalLayer = layer;
            Priority = priority;
            ValueProvider = provider;
            OwnerMod = ownerMod;
            IsIndexable = isIndexable;
            KeyEmbedding = keyEmbedding;
            AdaptivePriority = priority;
        }

        public float GetEffectivePriority()
        {
            return (Priority + AdaptivePriority) / 2f;
        }
    }

    public class ContextDiff
    {
        public static int DefaultLifetimeTicks => RimMindCoreMod.Settings?.contextDiffLifetimeTicks ?? 36000;

        public string Key = "";
        public ContextLayer Layer;
        public string OldValue = "";
        public string NewValue = "";
        public int InsertedTick;
        public int ExpireTick;

        public bool IsExpired(int currentTick) => currentTick >= ExpireTick;

        public string Format() => $"[{Key}] {OldValue} -> {NewValue}";
    }

    public static class SemanticEmbedding
    {
        public static void InvalidateNpc(string npcId) { }
        public static void InvalidateBlockEmbedding(string npcId, string key) { }
        public static void InvalidateEntryEmbeddings(string npcId, string key) { }
        public static float[]? GetBlockEmbedding(string npcId, string key) => null;
    }

    public class EmbeddingSnapshotStore
    {
        public void Record(EmbeddingSnapshotRecord record) { }
        public void Flush() { }
    }

    public class EmbeddingSnapshotRecord
    {
        public string NpcId = "";
        public string ScenarioId = "";
        public string Key = "";
        public string Layer = "";
        public string SourceText = "";
        public float[] Vector = Array.Empty<float>();
        public float RelevanceScore;
        public long TimestampTicks;
    }

    public class BudgetSchedulerConfig
    {
        public float W1 = 0.4f;
        public float W2 = 0.6f;
        public float Alpha = 0.01f;
        public float AlphaSmooth = 0.7f;
        public float PromoteThreshold = 0.8f;
        public float DemoteThreshold = 0.2f;
    }

    public static class ContextEntryQuery
    {
        public static int ExtractHour(IReadOnlyList<ContextEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e.Metadata != null && e.Metadata.TryGetValue("key", out var k) && k == "time")
                {
                    if (e.Metadata.TryGetValue("hour", out var h) && int.TryParse(h, out var hour))
                        return hour;
                }
            }
            return 12;
        }

        public static int ExtractColonistCount(IReadOnlyList<ContextEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e.Metadata != null && e.Metadata.TryGetValue("key", out var k) && k == "colonistCount")
                {
                    if (e.Metadata.TryGetValue("count", out var c) && int.TryParse(c, out var count))
                        return count;
                }
            }
            return 0;
        }
    }

    public static class SchemaRegistry
    {
        public static string AgentDecision = "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\"},\"reason\":{\"type\":\"string\"}}}";
    }
}

namespace RimMind.Core.Client
{
    public enum AIRequestState
    {
        Queued,
        Processing,
        Completed,
        Error,
        Cancelled
    }

    public enum AIRequestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public static class JsonRepairHelper
    {
        public static string Repair(string json) => json;

        public static string? TryRepairTruncatedJson(string? input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var trimmed = input.TrimEnd();
            if (string.IsNullOrEmpty(trimmed)) return null;

            try
            {
                Newtonsoft.Json.Linq.JToken.Parse(trimmed);
                return null;
            }
            catch { }

            string repaired = trimmed;

            if (repaired.EndsWith(",")) repaired = repaired.Substring(0, repaired.Length - 1);

            int openBraces = 0, openBrackets = 0;
            bool inString = false;
            char prev = '\0';
            foreach (char c in repaired)
            {
                if (prev != '\\' && c == '"') inString = !inString;
                if (!inString)
                {
                    if (c == '{') openBraces++;
                    else if (c == '}') openBraces--;
                    else if (c == '[') openBrackets++;
                    else if (c == ']') openBrackets--;
                }
                prev = c;
            }

            if (inString) repaired += "\"";

            for (int i = 0; i < openBrackets; i++) repaired += "]";
            for (int i = 0; i < openBraces; i++) repaired += "}";

            return repaired;
        }
    }

    public class QuotaExceededException : Exception
    {
        public QuotaExceededException() : base("quota exceeded") { }
        public QuotaExceededException(string message) : base(message) { }

        public static bool IsQuotaError(string? error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            var lower = error.ToLowerInvariant();
            return lower.Contains("quota") || lower.Contains("429")
                || lower.Contains("insufficient_balance") || lower.Contains("payment_required")
                || lower.Contains("quotaexceeded");
        }
    }

    public interface IAIClient
    {
        Task<AIResponse> SendAsync(AIRequest request);
        bool IsLocalEndpoint { get; }
    }
}

namespace RimMind.Core.Prompt
{
    public class PromptBudget
    {
        private readonly int _totalBudget;
        private readonly int _reserveForOutput;

        public PromptBudget(int totalBudget = 800, int reserveForOutput = 200)
        {
            _totalBudget = totalBudget;
            _reserveForOutput = reserveForOutput;
        }

        public int AvailableForInput => _totalBudget - _reserveForOutput;

        public List<PromptSection>? Compose(List<PromptSection>? sections)
        {
            if (sections == null) return null;
            if (sections.Count == 0) return new List<PromptSection>();

            int available = AvailableForInput;
            if (available <= 0) available = _totalBudget;

            var sorted = sections.OrderBy(s => s.Priority).ToList();
            var result = new List<PromptSection>();
            int used = 0;

            foreach (var section in sorted)
            {
                int tokens = section.EstimatedTokens;
                if (used + tokens <= available)
                {
                    result.Add(section);
                    used += tokens;
                }
                else if (section.IsCompressible && section.Compress != null)
                {
                    var compressed = section.Clone();
                    compressed.Content = section.Compress(section.Content);
                    compressed.EstimatedTokens = PromptSection.EstimateTokens(compressed.Content);
                    if (used + compressed.EstimatedTokens <= available)
                    {
                        result.Add(compressed);
                        used += compressed.EstimatedTokens;
                    }
                }
            }

            return result;
        }

        public string ComposeToString(List<PromptSection> sections)
        {
            var composed = Compose(sections);
            if (composed == null) return "";
            return string.Join("\n\n", composed.Select(s => s.Content ?? ""));
        }
    }

    public class ContextComposer
    {
        public static string ComposeSystemPrompt(List<Context.ContextEntry> entries)
        {
            return string.Join("\n", entries.Where(e => !string.IsNullOrEmpty(e.Content)).Select(e => e.Content));
        }

        public static List<PromptSection>? Reorder(List<PromptSection>? sections)
        {
            if (sections == null) return null;
            return sections.OrderBy(s => s.Priority).ThenBy(s => s.Tag).ToList();
        }

        public static string BuildFromSections(List<PromptSection> sections)
        {
            if (sections == null || sections.Count == 0) return "";
            if (sections.Count == 1) return sections[0].Content ?? "";
            return string.Join("\n", sections.Select(s => s.Content ?? ""));
        }

        public static string? CompressHistory(string? history, int maxLines, string? summaryLine = null)
        {
            if (history == null) return null;
            if (history.Length == 0) return "";
            var lines = history.Split('\n');
            if (lines.Length <= maxLines) return history;
            var kept = lines.Skip(lines.Length - maxLines).ToList();
            if (summaryLine != null)
                kept.Insert(0, summaryLine);
            return string.Join("\n", kept);
        }
    }
}

namespace RimMind.Core.Extensions
{
    public interface IParameterTuner
    {
        string TunerId { get; }
        void Tune(Context.BudgetSchedulerConfig config);
    }

    public interface IAudioPlayer
    {
        void Play(string clipPath);
    }

    public interface ISensorProvider
    {
        List<Client.StructuredTool> BuildTools(Verse.Pawn pawn);
    }
}

namespace RimMind.Core.Extensions.Sensor
{
    public class AgentToolDefinition
    {
        public string Name = "";
        public string Description = "";
        public string? Parameters;
    }
}

namespace RimMind.Core.Flywheel
{
    public enum FlywheelAutoApplyMode
    {
        Off,
        LogOnly,
        Safe,
        ApplyWithLog,
        Aggressive
    }

    public class FlywheelAnalysisReport
    {
        public string Id = "";
        public long TimestampTicks;
        public List<ParameterRecommendation> Recommendations = new List<ParameterRecommendation>();
        public Dictionary<string, float> Metrics = new Dictionary<string, float>();
    }

    public class AnalysisRecommendationEntry : ParameterRecommendation
    {
        public string TriggerRule = "";
        public string ComputationDetail = "";
        public bool Applied;
        public long ApplyTimestampTicks;
    }

    public class AnalysisReportRecord
    {
        public string AnalysisWindow = "";
        public int TotalRecords;
        public Dictionary<string, float> ComputedMetrics = new Dictionary<string, float>();
        public List<AnalysisRecommendationEntry> Recommendations = new List<AnalysisRecommendationEntry>();
        public long GeneratedAtTicks;
    }

    public static class FlywheelAnalysisReportWriter
    {
        public static void Write(AnalysisReportRecord report) { }
    }
}

namespace RimMind.Core.Npc
{
    public class NpcCommand
    {
        public string Name = "";
        public string Description = "";
    }

    public class NpcProfile : Verse.IExposable
    {
        public string NpcId = "";
        public string Name = "";
        public string ShortName = "";
        public string CharacterDescription = "";
        public string SystemPrompt = "";
        public List<NpcCommand> Commands = new List<NpcCommand>();

        public void ExposeData()
        {
            Verse.Scribe_Values.Look(ref NpcId, "npcId");
            Verse.Scribe_Values.Look(ref Name, "name");
            Verse.Scribe_Values.Look(ref ShortName, "shortName");
            Verse.Scribe_Values.Look(ref CharacterDescription, "characterDescription");
            Verse.Scribe_Values.Look(ref SystemPrompt, "systemPrompt");
        }
    }

    public class NpcChatResult
    {
        public string? Error;
        public string? Response;
    }
}

namespace RimMind.Core.Internal
{
    public class PawnData
    {
        public string? Name;
        public int Age;
        public string? GenderLabel;
        public string? RaceLabel;
        public string? ChildhoodTitle;
        public string? AdulthoodTitle;
        public List<string> TraitLabels = new List<string>();
        public float MoodPercent;
        public string? MoodString;
        public bool InMentalState;
        public string? MentalStateInspectLine;
        public bool Downed;
        public bool MentalBreakImminent;
        public List<(string Thought, float Offset)> MoodThoughts = new List<(string, float)>();
        public List<HediffRecord> Hediffs = new List<HediffRecord>();
        public List<(string Capacity, float Level)> Capacities = new List<(string, float)>();
        public List<(string Skill, int Level)> Skills = new List<(string, int)>();
        public string? CurrentJobReport;
        public string? CurrentJobDefLabel;
        public List<(int Priority, string WorkType)> WorkPriorities = new List<(int, string)>();
        public string? WeaponLabel;
        public List<string> ApparelLabels = new List<string>();
        public Dictionary<string, int> InventoryItems = new Dictionary<string, int>();
        public string? RoomLabel;
        public float Temperature;
        public bool HasMap;
        public List<(string Type, string Target)> Relations = new List<(string, string)>();
        public bool InCombat;
        public bool Drafted;
        public string? EnemyTargetLabel;
        public float? EnemyTargetHpPercent;
        public string? IdeologyName;
        public string? IdeologyMemes;
        public List<string> NotableGenes = new List<string>();
        public List<string> NearbyPawnNames = new List<string>();
        public int ColonistCount;
        public float ColonyWealth;
        public int ThreatCount;
        public string? WeatherLabel;
        public string? TimeString;
        public int TimeHour;
        public int TimeDay;
        public string? SeasonLabel;
    }

    public class HediffRecord
    {
        public string? PartLabel;
        public string? HediffLabel;
        public bool IsBad;
        public float Severity;
        public bool Visible;
    }

    public static class PawnDataExtractor
    {
        public static PawnData Extract(Verse.Pawn pawn) => new PawnData();
    }

    public static class GameContextBuilder
    {
        public static List<Context.ContextEntry> BuildMapContextEntries(Verse.Map map) => new List<Context.ContextEntry>();
        public static string ExtractPawnBaseInfo(Verse.Pawn pawn) => "";
        public static string ExtractFixedRelations(Verse.Pawn pawn) => "";
        public static string ExtractIdeology(Verse.Pawn pawn) => "";
        public static string ExtractSkillsSummary(Verse.Pawn pawn) => "";
        public static string ExtractCurrentArea(Verse.Pawn pawn) => "";
        public static string ExtractWeather(Verse.Pawn pawn) => "";
        public static string ExtractTimeOfDay(Verse.Pawn pawn) => "";
        public static string ExtractNearbyPawns(Verse.Pawn pawn) => "";
        public static string ExtractSeason(Verse.Pawn pawn) => "";
        public static string ExtractColonyStatus(Verse.Pawn pawn) => "";
        public static string ExtractHealth(Verse.Pawn pawn) => "";
        public static string ExtractMood(Verse.Pawn pawn) => "";
        public static string ExtractCurrentJob(Verse.Pawn pawn) => "";
        public static string ExtractCombatStatus(Verse.Pawn pawn) => "";
        public static string ExtractTargetInfo(Verse.Pawn pawn) => "";
    }
}

namespace RimMind.Core.Client
{
    public static class HttpHelper
    {
        public sealed class HttpException : Exception
        {
            public long StatusCode { get; }

            public HttpException(long statusCode, string message) : base(message)
            {
                StatusCode = statusCode;
            }
        }
    }
}

namespace RimMind.Core.Sensor
{
    using Verse;

    public class SensorManager : GameComponent
    {
        public static SensorManager? Instance;

        public SensorManager() { }
        public SensorManager(Game game) : base(game) { }

        public List<Client.StructuredTool> BuildAgentTools(Pawn pawn) => new List<Client.StructuredTool>();
    }
}

namespace RimMind.Core.Comps
{
    using Verse;

    public class CompProperties_PawnAgent : CompProperties
    {
        public CompProperties_PawnAgent()
        {
            compClass = typeof(CompPawnAgent);
        }
    }

    public class CompPawnAgent : ThingComp
    {
        public Agent.PawnAgent? Agent { get; set; }

        public static HashSet<int> ActivePawnIds = new HashSet<int>();

        public static CompPawnAgent? GetComp(Pawn pawn)
        {
            return pawn?.GetComp<CompPawnAgent>();
        }

        public static bool IsAgentActive(Pawn pawn)
        {
            var comp = GetComp(pawn);
            return comp?.Agent?.IsActive == true;
        }
    }
}

namespace RimMind.Core
{
    public static class ThreatClassifier
    {
        public static float Classify(Verse.Pawn pawn) => 0f;

        public static string ClassifyThreatTier(float wealth, float high, float medium, float low, float threatScale)
        {
            float scale = threatScale <= 0f ? 1f : threatScale;
            float effectiveLow = low / scale;
            float effectiveMedium = medium / scale;
            float effectiveHigh = high / scale;

            if (wealth >= effectiveHigh) return "Extreme";
            if (wealth >= effectiveMedium) return "High";
            if (wealth >= effectiveLow) return "Medium";
            return "Low";
        }
    }
}

namespace RimMind.Core.Settings
{
    public static class ApiKeyObfuscator
    {
        public const string ObfuscationPrefix = "RM_OBF:";

        public static string? Obfuscate(string? apiKey)
        {
            if (apiKey == null) return null;
            if (apiKey.Length == 0) return string.Empty;
            var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
            return ObfuscationPrefix + Convert.ToBase64String(bytes);
        }

        public static string? Deobfuscate(string? obfuscated)
        {
            if (obfuscated == null) return null;
            if (obfuscated.Length == 0) return string.Empty;
            if (!obfuscated.StartsWith(ObfuscationPrefix)) return obfuscated;
            var base64 = obfuscated.Substring(ObfuscationPrefix.Length);
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return obfuscated;
            }
        }
    }
}
