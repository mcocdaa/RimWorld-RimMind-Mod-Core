using RimMind.Contracts.Npc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Contracts.Result;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Client;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Queue;
using RimMind.Kernel.Llm;
using RimMind.Core.Agent;
using RimMind.Core.Npc;
using RimMind.Core;
using Verse;

namespace RimMind.Adapters.Client.Player2
{
    public class Player2StorageDriver : IStorageDriver
    {
        private readonly Player2Client _client;
        private readonly string _gameId;
        private readonly INpcManager _npcManager;

        private readonly List<LocalMemoryEntry> _localMemoryIndex = new List<LocalMemoryEntry>();
        private readonly object _indexLock = new object();

        private struct LocalMemoryEntry
        {
            public string Key;
            public string Value;
        }

        public bool AutoDispatch { get; set; } = false;

        public bool IsRemote => true;
        public bool SupportsStreaming => true;
        public bool SupportsTts => true;
        public bool SupportsCommands => true;
        public bool SupportsStructuredOutput => true;

        public Player2StorageDriver(Player2Client client, INpcManager npcManager)
        {
            _client = client;
            _npcManager = npcManager;
            _gameId = Player2Client.GameClientId;
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null)
        {
            try
            {
                var request = new AIRequest
                {
                    NpcId = npcId,
                    UserPrompt = message,
                    SystemPrompt = context,
                };
                var aiResult = await _client.SendAsync(request);
                if (aiResult.IsErr)
                    return Result<NpcChatResult, RimMindError>.Err(aiResult.Error);
                return Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult(npcId, aiResult.Value.Content ?? ""));
            }
            catch (Exception ex)
            {
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.StorageDriverFailed(ex.Message, ex));
            }
        }

        public async Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            if (profile == null) return Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed("Profile is null"));
            try
            {
                var body = new
                {
                    npc_id = profile.NpcId,
                    name = profile.Name,
                    short_name = profile.ShortName,
                    character_description = profile.CharacterDescription,
                    system_prompt = profile.SystemPrompt,
                    commands = ConvertCommands(profile.Commands),
                    tts = profile.TtsConfig != null ? new
                    {
                        voice_ids = profile.TtsConfig.VoiceIds,
                        speed = profile.TtsConfig.Speed,
                        audio_format = profile.TtsConfig.AudioFormat,
                    } : null,
                };
                string json = JsonConvert.SerializeObject(body, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await _client.SendRawAsync("/npcs/spawn", json);
                if (!response.IsOk)
                    return Result<bool, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("SpawnNpc raw request failed"));
                return Result<bool, RimMindError>.Ok(true);
            }
            catch (Exception ex) { return Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"SpawnNpcAsync failed: {ex.Message}", ex)); }
        }

        public async Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            try
            {
                var response = await _client.SendRawAsync($"/npcs/{npcId}/kill", "{}");
                if (!response.IsOk)
                    return Result<bool, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("KillNpc raw request failed"));
                return Result<bool, RimMindError>.Ok(true);
            }
            catch (Exception ex) { return Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"KillNpcAsync failed: {ex.Message}", ex)); }
        }

        public bool IsNpcAlive(string npcId)
        {
            return _npcManager?.IsNpcAlive(npcId) == true;
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
        {
            try
            {
                if (_client == null || !_client.IsConfigured())
                    return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.ClientNotConfigured(nameof(Player2StorageDriver)));

                var body = new
                {
                    npc_id = snapshot.NpcId,
                    scenario = snapshot.Scenario,
                    messages = snapshot.Messages,
                    max_tokens = snapshot.MaxTokens,
                    temperature = snapshot.Temperature,
                    current_query = snapshot.CurrentQuery,
                };
                string json = JsonConvert.SerializeObject(body, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await _client.SendRawAsync($"/npcs/{snapshot.NpcId}/chat", json);

                if (!response.IsOk)
                    return Result<NpcChatResult, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("Raw request failed"));

                var result = JsonConvert.DeserializeObject<NpcChatResult>(response.Content!);
                result ??= new NpcChatResult { Message = response.Content ?? "" };
                MaybeDispatch(result, snapshot.NpcId);
                return Result<NpcChatResult, RimMindError>.Ok(result);
            }
            catch (System.Exception ex)
            {
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Player2StorageDriver.ChatAsync failed for '{snapshot.NpcId}': {ex.Message}", isWarning: true);
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.StorageDriverFailed(ex.Message, ex));
            }
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrEmpty(gameStateInfo))
                {
                    var engine = RimMindAPI.GetContextEngine();
                    if (engine != null)
                    {
                        var request = new ContextRequest
                        {
                            NpcId = npcId,
                            Scenario = ScenarioIds.Dialogue,
                            Budget = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f,
                            CurrentQuery = message,
                            MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                            Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
                            Map = Find.CurrentMap,
                        };
                        var snapshot = engine.BuildSnapshot(request);
                        var sb = new StringBuilder();
                        foreach (var msg in snapshot.Messages)
                        {
                            if (msg.Role == "system" && !string.IsNullOrEmpty(msg.Content))
                                sb.AppendLine(msg.Content);
                        }
                        gameStateInfo = sb.ToString().TrimEnd();
                    }
                    else
                    {
                        gameStateInfo = GameContextBuilder.CollectBasicGameState(npcId);
                    }
                }

                var body = new
                {
                    sender_name = sender,
                    message = message,
                    game_state_info = gameStateInfo,
                };
                string json = JsonConvert.SerializeObject(body, Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await _client.SendRawAsync($"/npcs/{npcId}/chat", json);

                if (!response.IsOk)
                    return Result<NpcChatResult, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("Raw request failed"));

                var result = JsonConvert.DeserializeObject<NpcChatResult>(response.Content!);
                result ??= new NpcChatResult { Message = response.Content ?? "" };
                MaybeDispatch(result, npcId);
                return Result<NpcChatResult, RimMindError>.Ok(result);
            }
            catch (System.Exception ex)
            {
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.StorageDriverFailed(ex.Message, ex));
            }
        }

        public async IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            var snapshot = new ContextSnapshot
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Dialogue,
                CurrentQuery = gameStateInfo != null ? $"{message}\n\n[Game State]\n{gameStateInfo}" : message,
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
            };
            snapshot.AddMessage(new ChatMessage { Role = "user", Content = snapshot.CurrentQuery });

            var result = await ChatAsync(snapshot, ct);
            if (result.IsErr)
            {
                yield return Result<NpcChatChunk, RimMindError>.Err(result.Error);
                yield break;
            }

            var chatResult = result.Value;
            if (chatResult.Message != null)
                onChunk?.Invoke(chatResult.Message);

            yield return Result<NpcChatChunk, RimMindError>.Ok(new NpcChatChunk(npcId, chatResult.Message ?? "", chatResult.Emotion, isFinal: true));
        }

        public async Task<Result<string, RimMindError>> GetHistoryAsync(string npcId, int limit = 50)
        {
            try
            {
                var response = await _client.GetRawAsync($"/npcs/{npcId}/history?limit={limit}");
                if (!response.IsOk)
                    return Result<string, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("GetHistory raw request failed"));
                return Result<string, RimMindError>.Ok(response.Content ?? "");
            }
            catch (Exception ex) { return Result<string, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"GetHistoryAsync failed: {ex.Message}", ex)); }
        }

        public async Task<Result<bool, RimMindError>> PutAsync(string key, string value)
        {
            lock (_indexLock)
            {
                int idx = _localMemoryIndex.FindIndex(e => e.Key == key);
                if (idx >= 0)
                    _localMemoryIndex[idx] = new LocalMemoryEntry { Key = key, Value = value };
                else
                    _localMemoryIndex.Add(new LocalMemoryEntry { Key = key, Value = value });
            }

            try
            {
                var body = new { value = value };
                string json = JsonConvert.SerializeObject(body);
                var response = await _client.SendRawAsync($"/games/{_gameId}/data/user/{key}", json);
                if (!response.IsOk)
                    return Result<bool, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("Put raw request failed"));
                return Result<bool, RimMindError>.Ok(true);
            }
            catch (Exception ex) { return Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"PutAsync failed: {ex.Message}", ex)); }
        }

        public async Task<Result<string?, RimMindError>> GetAsync(string key)
        {
            try
            {
                var response = await _client.GetRawAsync($"/games/{_gameId}/data/user/{key}");
                if (!response.IsOk)
                    return Result<string?, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("Get raw request failed"));
                return Result<string?, RimMindError>.Ok(response.Content);
            }
            catch (Exception ex) { return Result<string?, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"GetAsync failed: {ex.Message}", ex)); }
        }

        public async Task<Result<bool, RimMindError>> DeleteAsync(string key)
        {
            try
            {
                var response = await _client.DeleteRawAsync($"/games/{_gameId}/data/user/{key}");
                if (!response.IsOk)
                    return Result<bool, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("Delete raw request failed"));
                return Result<bool, RimMindError>.Ok(true);
            }
            catch (Exception ex) { return Result<bool, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"DeleteAsync failed: {ex.Message}", ex)); }
        }

        public async Task<Result<Dictionary<string, string>, RimMindError>> GetBatchAsync(IEnumerable<string> keys)
        {
            try
            {
                var body = new { keys = keys };
                string json = JsonConvert.SerializeObject(body);
                var response = await _client.SendRawAsync($"/games/{_gameId}/data/user/batch", json);
                if (!response.IsOk)
                    return Result<Dictionary<string, string>, RimMindError>.Err(response.Error ?? RimMindErrors.StorageDriverFailed("GetBatch raw request failed"));
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content!)
                    ?? new Dictionary<string, string>();
                return Result<Dictionary<string, string>, RimMindError>.Ok(result);
            }
            catch (Exception ex) { return Result<Dictionary<string, string>, RimMindError>.Err(RimMindErrors.StorageDriverFailed($"GetBatchAsync failed: {ex.Message}", ex)); }
        }

        public Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json)
        {
            return PutAsync("rimmind:all_memory_entries", json ?? string.Empty);
        }

        public Task<Result<string?, RimMindError>> LoadAllEntriesAsync()
        {
            return GetAsync("rimmind:all_memory_entries");
        }

        public Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(query)) return Task.FromResult(Result<List<string>, RimMindError>.Ok(results));

            var tokens = query.ToLowerInvariant().Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return Task.FromResult(Result<List<string>, RimMindError>.Ok(results));

            var scored = new List<(string value, int score)>();

            lock (_indexLock)
            {
                foreach (var entry in _localMemoryIndex)
                {
                    if (string.IsNullOrEmpty(entry.Value)) continue;
                    int score = 0;
                    var lowerValue = entry.Value.ToLowerInvariant();
                    foreach (var token in tokens)
                    {
                        if (lowerValue.Contains(token))
                            score++;
                    }
                    if (score > 0)
                        scored.Add((entry.Value, score));
                }
            }

            results = scored
                .OrderByDescending(s => s.score)
                .Take(limit)
                .Select(s => s.value)
                .ToList();

            return Task.FromResult(Result<List<string>, RimMindError>.Ok(results));
        }

        private static List<object> ConvertCommands(List<NpcCommand> commands)
        {
            var result = new List<object>();
            if (commands == null) return result;
            foreach (var cmd in commands)
            {
                result.Add(new
                {
                    name = cmd.Name,
                    description = cmd.Description,
                    parameters = cmd.Parameters != null ? JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(cmd.Parameters)) : null,
                    never_respond_with_message = cmd.NeverRespondWithMessage,
                });
            }
            return result;
        }

        private void MaybeDispatch(NpcChatResult result, string npcId)
        {
            if (!AutoDispatch) return;
            try
            {
                ResponseDispatcher.Dispatch(result);
            }
            catch (System.Exception ex)
            {
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Player2StorageDriver: auto-dispatch failed for '{npcId}' - {ex.Message}", isWarning: true);
            }
        }
    }
}
