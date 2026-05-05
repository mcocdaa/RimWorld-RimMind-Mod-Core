using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Core.Client;
using RimMind.Core.Context;
using RimMind.Core.Settings;
using Verse;

namespace RimMind.Core.Npc
{
    public class LocalStorageDriver : IStorageDriver
    {
        private readonly Dictionary<string, NpcProfile> _npcRegistry = new Dictionary<string, NpcProfile>();
        internal static readonly ConcurrentDictionary<string, string> KvStore = new ConcurrentDictionary<string, string>();
        private readonly HistoryManager _historyManager;
        private readonly string _keyPrefix;

        public bool IsRemote => false;
        public bool SupportsStreaming => false;
        public bool SupportsTts => false;
        public bool SupportsCommands => true;
        public bool SupportsStructuredOutput => true;

        public LocalStorageDriver(HistoryManager historyManager, string keyPrefix = "core")
        {
            _historyManager = historyManager;
            _keyPrefix = keyPrefix + ":";
        }

        private string PrefixKey(string key) => _keyPrefix + key;

        public Task<bool> SpawnNpcAsync(NpcProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.NpcId)) return Task.FromResult(false);
            _npcRegistry[profile.NpcId] = profile;
            return Task.FromResult(true);
        }

        public Task<bool> KillNpcAsync(string npcId)
        {
            _npcRegistry.Remove(npcId);
            _historyManager.ClearHistory(npcId);
            return Task.FromResult(true);
        }

        public bool IsNpcAlive(string npcId) => _npcRegistry.ContainsKey(npcId);

        public async Task<NpcChatResult> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
        {
            if (!_npcRegistry.TryGetValue(snapshot.NpcId, out var profile))
                return new NpcChatResult { Error = $"NPC {snapshot.NpcId} not found" };

            var request = new AIRequest
            {
                Messages = new List<ChatMessage>(snapshot.Messages),
                MaxTokens = snapshot.MaxTokens,
                Temperature = snapshot.Temperature,
                RequestId = $"NpcChat_{snapshot.NpcId}_{Find.TickManager.TicksGame}",
                ModId = "NpcChat",
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true,
                Priority = AIRequestPriority.Normal,
            };

            var client = RimMindAPI.GetClient();
            if (client == null)
                return new NpcChatResult { Error = "AI client not configured." };

            AIResponse response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                return new NpcChatResult { Error = $"AI request failed: {ex.Message}" };
            }

            if (!response.Success)
                return new NpcChatResult { Error = response.Error };

            string content = response.Content ?? "";

            content = ExtractReplyField(content);

            var commands = ParseCommands(content);

            string? userMessage = snapshot.CurrentQuery;
            _historyManager.AddTurn(snapshot.NpcId, userMessage ?? "", content, snapshot.Scenario);
            _historyManager.CompressIfNeeded(snapshot.NpcId);

            return new NpcChatResult { Message = content, Commands = commands };
        }

        public async Task<NpcChatResult> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
        {
            var request = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Dialogue,
                Budget = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f,
                CurrentQuery = message,
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
            };
            var engine = RimMindAPI.GetContextEngine();
            var snapshot = engine.BuildSnapshot(request);
            return await ChatAsync(snapshot, ct);
        }

        public async Task<NpcChatResult> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, CancellationToken ct = default)
        {
            var request = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Dialogue,
                Budget = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f,
                CurrentQuery = message,
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
            };
            var engine = RimMindAPI.GetContextEngine();
            var snapshot = engine.BuildSnapshot(request);
            var result = await ChatAsync(snapshot, ct);
            if (result.Message != null)
                onChunk?.Invoke(result.Message);
            return result;
        }

        public Task<string> GetHistoryAsync(string npcId, int limit = 50)
        {
            var history = _historyManager.GetHistory(npcId, limit);
            if (history == null || history.Count == 0)
                return Task.FromResult("");

            var sb = new System.Text.StringBuilder();
            foreach (var (role, content) in history)
                sb.AppendLine($"[{role}] {content}");
            return Task.FromResult(sb.ToString().TrimEnd());
        }

        public Task<bool> PutAsync(string key, string value) { KvStore[PrefixKey(key)] = value; return Task.FromResult(true); }
        public Task<string?> GetAsync(string key) { KvStore.TryGetValue(PrefixKey(key), out var v); return Task.FromResult<string?>(v); }
        public Task<bool> DeleteAsync(string key) { KvStore.TryRemove(PrefixKey(key), out _); return Task.FromResult(true); }
        public Task<Dictionary<string, string>> GetBatchAsync(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, string>();
            foreach (var k in keys)
                if (KvStore.TryGetValue(PrefixKey(k), out var v))
                    result[k] = v;
            return Task.FromResult(result);
        }

        private const string AllEntriesKey = "rimmind:all_memory_entries";

        public Task<bool> SaveAllEntriesAsync(string json)
        {
            KvStore[PrefixKey(AllEntriesKey)] = json ?? string.Empty;
            return Task.FromResult(true);
        }

        public Task<string?> LoadAllEntriesAsync()
        {
            KvStore.TryGetValue(PrefixKey(AllEntriesKey), out var json);
            return Task.FromResult<string?>(json);
        }

        public Task<List<string>> QueryMemoriesAsync(string npcId, string query, int limit = 10)
        {
            var results = new List<string>();
            var history = _historyManager.GetHistory(npcId, 20);
            if (history == null || history.Count == 0)
                return Task.FromResult(results);

            var queryLower = query?.ToLowerInvariant() ?? "";
            foreach (var (role, content) in history)
            {
                if (results.Count >= limit) break;
                if (string.IsNullOrEmpty(content)) continue;
                if (!string.IsNullOrEmpty(queryLower) && content.ToLowerInvariant().Contains(queryLower))
                    results.Add($"[{role}] {content}");
            }
            return Task.FromResult(results);
        }

        /// <summary>
        /// �� JSON ��Ӧ����ȡ reply �ֶ��ı�
         /// </summary>
         private static string ExtractReplyField(string content)
        {
            if (string.IsNullOrEmpty(content) || !content.TrimStart().StartsWith("{")) return content;
            try
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(content);
                if (obj != null && obj.TryGetValue("reply", out var msg) && msg != null)
                {
                    string? extracted = msg.ToString();
                    if (!string.IsNullOrEmpty(extracted)) return extracted;
                }
            }
            catch (Exception ex) { Log.Warning($"[RimMind-Core] Failed to extract message from JSON: {ex.Message}"); }
            return content;
        }

        private static readonly Regex _commandRegex = new Regex(@"\[CMD:(\w+)(?::([^\]]+))?\]", RegexOptions.Compiled);

        private static List<NpcCommandResult> ParseCommands(string content)
        {
            var commands = new List<NpcCommandResult>();
            if (string.IsNullOrEmpty(content)) return commands;

            var matches = _commandRegex.Matches(content);
            foreach (Match match in matches)
            {
                commands.Add(new NpcCommandResult
                {
                    Name = match.Groups[1].Value,
                    Arguments = match.Groups[2].Success ? match.Groups[2].Value : "",
                });
            }
            return commands;
        }
    }
}
