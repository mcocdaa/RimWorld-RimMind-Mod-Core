using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Npc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Infrastructure.Persistence
{
    public class LocalStorageDriver : IStorageDriver
    {
        private readonly Dictionary<string, NpcProfile> _npcRegistry = new Dictionary<string, NpcProfile>();
        internal static readonly ConcurrentDictionary<string, string> KvStore = new ConcurrentDictionary<string, string>();
        private readonly IHistoryManager _historyManager;
        private readonly string _keyPrefix;

        public bool IsRemote => false;
        public bool SupportsStreaming => false;
        public bool SupportsTts => false;
        public bool SupportsCommands => true;
        public bool SupportsStructuredOutput => true;

        public LocalStorageDriver(IHistoryManager historyManager, string keyPrefix = "core")
        {
            _historyManager = historyManager;
            _keyPrefix = keyPrefix + ":";
        }

        private string PrefixKey(string key) => _keyPrefix + key;

        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null)
        {
            return Task.FromResult(Result<NpcChatResult, RimMindError>.Err(
                RimMindErrors.StorageDriverFailed("Local storage does not support chat")));
        }

        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.NpcId))
                return Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed("Profile or NpcId is null")));
            _npcRegistry[profile.NpcId] = profile;
            return Task.FromResult(Result<bool, RimMindError>.Ok(true));
        }

        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            _npcRegistry.Remove(npcId);
            _historyManager.ClearHistory(npcId);
            return Task.FromResult(Result<bool, RimMindError>.Ok(true));
        }

        public bool IsNpcAlive(string npcId) => _npcRegistry.ContainsKey(npcId);

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
        {
            if (!_npcRegistry.TryGetValue(snapshot.NpcId, out var profile))
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.NpcNotFound(snapshot.NpcId));

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
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.ClientNotConfigured(nameof(LocalStorageDriver)));

            Result<AIResponse, RimMindError> aiResult;
            try
            {
                aiResult = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"AI request failed: {ex.Message}", ex));
            }

            if (aiResult.IsErr)
                return Result<NpcChatResult, RimMindError>.Err(aiResult.Error);

            string content = aiResult.Value.Content ?? "";

            content = ExtractReplyField(content);

            var commands = ParseCommands(content);

            string? userMessage = snapshot.CurrentQuery;
            _historyManager.AddTurn(snapshot.NpcId, userMessage ?? "", content, snapshot.Scenario);
            _historyManager.CompressIfNeeded(snapshot.NpcId);

            return Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult { Message = content, Commands = commands });
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
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

        public async IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
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
            if (result.TryGetValue(out var chatResult) && chatResult.Message != null)
                onChunk?.Invoke(chatResult.Message);
            if (result.IsOk)
                yield return Result<NpcChatChunk, RimMindError>.Ok(new NpcChatChunk(npcId, chatResult.Message ?? "", chatResult.Emotion, isFinal: true));
            else
                yield return Result<NpcChatChunk, RimMindError>.Err(result.Error);
        }

        public Task<Result<string, RimMindError>> GetHistoryAsync(string npcId, int limit = 50)
        {
            var history = _historyManager.GetHistory(npcId, limit);
            if (history == null || history.Count == 0)
                return Task.FromResult(Result<string, RimMindError>.Ok(""));

            var sb = new System.Text.StringBuilder();
            foreach (var (role, content) in history)
                sb.AppendLine($"[{role}] {content}");
            return Task.FromResult(Result<string, RimMindError>.Ok(sb.ToString().TrimEnd()));
        }

        public Task<Result<bool, RimMindError>> PutAsync(string key, string value) { KvStore[PrefixKey(key)] = value; return Task.FromResult(Result<bool, RimMindError>.Ok(true)); }
        public Task<Result<string?, RimMindError>> GetAsync(string key) { KvStore.TryGetValue(PrefixKey(key), out var v); return Task.FromResult(Result<string?, RimMindError>.Ok(v)); }
        public Task<Result<bool, RimMindError>> DeleteAsync(string key) { KvStore.TryRemove(PrefixKey(key), out _); return Task.FromResult(Result<bool, RimMindError>.Ok(true)); }
        public Task<Result<Dictionary<string, string>, RimMindError>> GetBatchAsync(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, string>();
            foreach (var k in keys)
                if (KvStore.TryGetValue(PrefixKey(k), out var v))
                    result[k] = v;
            return Task.FromResult(Result<Dictionary<string, string>, RimMindError>.Ok(result));
        }

        private const string AllEntriesKey = "rimmind:all_memory_entries";

        public Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json)
        {
            KvStore[PrefixKey(AllEntriesKey)] = json ?? string.Empty;
            return Task.FromResult(Result<bool, RimMindError>.Ok(true));
        }

        public Task<Result<string?, RimMindError>> LoadAllEntriesAsync()
        {
            KvStore.TryGetValue(PrefixKey(AllEntriesKey), out var json);
            return Task.FromResult(Result<string?, RimMindError>.Ok(json));
        }

        public Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10)
        {
            var results = new List<string>();
            var history = _historyManager.GetHistory(npcId, 20);
            if (history == null || history.Count == 0)
                return Task.FromResult(Result<List<string>, RimMindError>.Ok(results));

            var queryLower = query?.ToLowerInvariant() ?? "";
            foreach (var (role, content) in history)
            {
                if (results.Count >= limit) break;
                if (string.IsNullOrEmpty(content)) continue;
                if (!string.IsNullOrEmpty(queryLower) && content.ToLowerInvariant().Contains(queryLower))
                    results.Add($"[{role}] {content}");
            }
            return Task.FromResult(Result<List<string>, RimMindError>.Ok(results));
        }

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
            catch (Exception ex) { RimMindErrors.Warn($"[RimMind-Core] Failed to extract message from JSON: {ex.Message}"); }
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
                    Arguments = match.Groups[2].Success ? new[] { match.Groups[2].Value } : new string[0],
                });
            }
            return commands;
        }
    }
}
