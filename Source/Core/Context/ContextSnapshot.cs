using System.Collections.Generic;
using RimMind.Core.Client;

namespace RimMind.Core.Context
{
    public class ContextSnapshot
    {
        public string NpcId = null!;
        public string Scenario = "";
        private List<ChatMessage> _messages = new List<ChatMessage>();
        public IReadOnlyList<ChatMessage> Messages => _messages;
        public List<StructuredTool>? Tools;
        public int EstimatedTokens;
        public ContextLayerMeta Meta = new ContextLayerMeta();
        public int MaxTokens = 800;
        public float Temperature = 0.7f;
        public string? CurrentQuery;
        public string[] IncludedKeys = new string[0];
        public string[] TrimmedKeys = new string[0];
        public float BudgetValue;
        private Dictionary<string, bool> _cacheHitEvents = new Dictionary<string, bool>();
        public IReadOnlyDictionary<string, bool> CacheHitEvents => _cacheHitEvents;
        public Dictionary<string, int> KeyChangeCounts = new Dictionary<string, int>();
        public Dictionary<string, float> KeyScores = new Dictionary<string, float>();
        public int DiffCount;
        public long BuildStartTicks;
        public Dictionary<string, long> LatencyByLayerMs = new Dictionary<string, long>();
        private List<ContextEntry> _allEntries = new List<ContextEntry>();
        public IReadOnlyList<ContextEntry> AllEntries => _allEntries;

        internal List<KeyMeta>? _commitFilteredKeys;
        internal BudgetAllocation? _commitSchedule;
        internal object? _commitPawn;

        internal void AddMessage(ChatMessage msg) => _messages.Add(msg);
        internal void InsertMessage(int index, ChatMessage msg) => _messages.Insert(index, msg);
        internal void SetMessages(List<ChatMessage> messages) => _messages = messages;
        internal void ClearMessages() => _messages.Clear();
        internal void AddEntry(ContextEntry entry) => _allEntries.Add(entry);
        internal void AddEntries(IEnumerable<ContextEntry> entries) => _allEntries.AddRange(entries);
        internal void SetCacheHitEvent(string key, bool value) => _cacheHitEvents[key] = value;
    }

    public class ContextLayerMeta
    {
        public int L0Tokens;
        public int L1Tokens;
        public int L2Tokens;
        public int L3Tokens;
        public int L4Tokens;
        public int L5Tokens;
        public int TotalTokens;
    }
}
