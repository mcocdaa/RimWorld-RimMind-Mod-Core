using RimMind.Contracts.Npc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Adapters.Client.Player2;
using RimMind.Contracts.Result;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Queue;

namespace RimMind.Core.Npc
{
    public class HybridStorageDriver : IStorageDriver
    {
        private readonly LocalStorageDriver _local;
        private readonly Player2StorageDriver _remote;

        public bool IsRemote => true;
        public bool SupportsStreaming => _remote.SupportsStreaming;
        public bool SupportsTts => _remote.SupportsTts;
        public bool SupportsCommands => _remote.SupportsCommands;
        public bool SupportsStructuredOutput => _remote.SupportsStructuredOutput;

        public HybridStorageDriver(Player2Client client, IHistoryManager historyManager)
        {
            _local = new LocalStorageDriver(historyManager);
            _remote = new Player2StorageDriver(client, RimMindServiceLocator.Get<INpcManager>());
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null)
        {
            return await _remote.ChatAsync(npcId, message, context);
        }

        public async Task<bool> SpawnNpcAsync(NpcProfile profile)
        {
            var localResult = await _local.SpawnNpcAsync(profile);
            try { await _remote.SpawnNpcAsync(profile); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote SpawnNpc failed: {ex.Message}", isWarning: true); }
            return localResult;
        }

        public async Task<bool> KillNpcAsync(string npcId)
        {
            var localResult = await _local.KillNpcAsync(npcId);
            try { await _remote.KillNpcAsync(npcId); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote KillNpc failed: {ex.Message}", isWarning: true); }
            return localResult;
        }

        public bool IsNpcAlive(string npcId)
        {
            return _local.IsNpcAlive(npcId) || _remote.IsNpcAlive(npcId);
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
        {
            var remoteResult = await _remote.ChatAsync(snapshot, ct);
            if (remoteResult.IsOk)
                return remoteResult;

            AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatAsync failed, falling back to local: {remoteResult.Error?.Message}", isWarning: true);
            return await _local.ChatAsync(snapshot, ct);
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
        {
            var remoteResult = await _remote.ChatAsync(npcId, sender, message, gameStateInfo, ct);
            if (remoteResult.IsOk)
                return remoteResult;

            AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatAsync(legacy) failed, falling back to local: {remoteResult.Error?.Message}", isWarning: true);
            return await _local.ChatAsync(npcId, sender, message, gameStateInfo, ct);
        }

        public async IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            var remoteResult = _remote.ChatStreamingAsync(npcId, sender, message, onChunk, gameStateInfo, ct);
            var hasError = false;
            RimMindError? remoteError = null;

            await foreach (var chunk in remoteResult.WithCancellation(ct))
            {
                if (chunk.IsErr)
                {
                    hasError = true;
                    remoteError = chunk.Error;
                    break;
                }
                yield return chunk;
            }

            if (hasError && remoteError != null)
            {
                AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatStreamingAsync failed, falling back to local: {remoteError.Message}", isWarning: true);
                var localResult = await _local.ChatAsync(npcId, sender, message, gameStateInfo, ct);
                if (localResult.IsOk)
                {
                    var chatResult = localResult.Value;
                    if (chatResult.Message != null)
                        onChunk?.Invoke(chatResult.Message);
                    yield return Result<NpcChatChunk, RimMindError>.Ok(new NpcChatChunk(npcId, chatResult.Message ?? "", chatResult.Emotion, isFinal: true));
                }
                else
                {
                    yield return Result<NpcChatChunk, RimMindError>.Err(localResult.Error);
                }
            }
        }

        public async Task<string> GetHistoryAsync(string npcId, int limit = 50)
        {
            var local = await _local.GetHistoryAsync(npcId, limit);
            if (!string.IsNullOrEmpty(local)) return local;
            try { return await _remote.GetHistoryAsync(npcId, limit); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote GetHistory failed: {ex.Message}", isWarning: true); return local; }
        }

        public async Task<bool> PutAsync(string key, string value)
        {
            var localResult = await _local.PutAsync(key, value);
            try { await _remote.PutAsync(key, value); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote Put failed: {ex.Message}", isWarning: true); }
            return localResult;
        }

        public async Task<string?> GetAsync(string key)
        {
            var local = await _local.GetAsync(key);
            if (local != null) return local;
            try { return await _remote.GetAsync(key); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote Get failed: {ex.Message}", isWarning: true); return null; }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var localResult = await _local.DeleteAsync(key);
            try { await _remote.DeleteAsync(key); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote Delete failed: {ex.Message}", isWarning: true); }
            return localResult;
        }

        public async Task<Dictionary<string, string>> GetBatchAsync(IEnumerable<string> keys)
        {
            var local = await _local.GetBatchAsync(keys);
            if (local != null && local.Count > 0) return local;
            try { return await _remote.GetBatchAsync(keys); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote GetBatch failed: {ex.Message}", isWarning: true); return local!; }
        }

        public async Task<bool> SaveAllEntriesAsync(string json)
        {
            var localResult = await _local.SaveAllEntriesAsync(json);
            try { await _remote.SaveAllEntriesAsync(json); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote SaveAllEntries failed: {ex.Message}", isWarning: true); }
            return localResult;
        }

        public async Task<string?> LoadAllEntriesAsync()
        {
            var local = await _local.LoadAllEntriesAsync();
            if (local != null) return local;
            try { return await _remote.LoadAllEntriesAsync(); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote LoadAllEntries failed: {ex.Message}", isWarning: true); return null; }
        }

        public async Task<List<string>> QueryMemoriesAsync(string npcId, string query, int limit = 10)
        {
            var local = await _local.QueryMemoriesAsync(npcId, query, limit);
            if (local != null && local.Count > 0) return local;
            try { return await _remote.QueryMemoriesAsync(npcId, query, limit); }
            catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] HybridDriver: remote QueryMemories failed: {ex.Message}", isWarning: true); return local!; }
        }
    }
}
