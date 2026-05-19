using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Npc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.Persistence;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    public class HybridStorageDriver : IStorageDriver
    {
        private readonly LocalStorageDriver _local;
        private readonly Player2StorageDriver _remote;
        private readonly ILogSink? _logSink;

        public bool IsRemote => true;
        public bool SupportsStreaming => _remote.SupportsStreaming;
        public bool SupportsTts => _remote.SupportsTts;
        public bool SupportsCommands => _remote.SupportsCommands;
        public bool SupportsStructuredOutput => _remote.SupportsStructuredOutput;

        public HybridStorageDriver(Player2Client client, IHistoryManager historyManager)
        {
            _local = new LocalStorageDriver(historyManager);
            _remote = new Player2StorageDriver(client, RimMindServiceLocator.Get<INpcManager>());
            _logSink = RimMindServiceLocator.Get<ILogSink>();
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null)
        {
            return await _remote.ChatAsync(npcId, message, context);
        }

        public async Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            var localResult = await _local.SpawnNpcAsync(profile);
            var remoteResult = await _remote.SpawnNpcAsync(profile);
            if (localResult.IsErr) return localResult;
            if (remoteResult.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote SpawnNpc failed: {remoteResult.Error}", isWarning: true);
            return localResult;
        }

        public async Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            var localResult = await _local.KillNpcAsync(npcId);
            var remoteResult = await _remote.KillNpcAsync(npcId);
            if (localResult.IsErr) return localResult;
            if (remoteResult.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote KillNpc failed: {remoteResult.Error}", isWarning: true);
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

            _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatAsync failed, falling back to local: {remoteResult.Error?.Message}", isWarning: true);
            return await _local.ChatAsync(snapshot, ct);
        }

        public async Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
        {
            var remoteResult = await _remote.ChatAsync(npcId, sender, message, gameStateInfo, ct);
            if (remoteResult.IsOk)
                return remoteResult;

            _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatAsync(legacy) failed, falling back to local: {remoteResult.Error?.Message}", isWarning: true);
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
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote ChatStreamingAsync failed, falling back to local: {remoteError.Message}", isWarning: true);
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

        public async Task<Result<string, RimMindError>> GetHistoryAsync(string npcId, int limit = 50)
        {
            var local = await _local.GetHistoryAsync(npcId, limit);
            if (local.IsOk && !string.IsNullOrEmpty(local.Value)) return local;
            var remote = await _remote.GetHistoryAsync(npcId, limit);
            if (remote.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote GetHistory failed: {remote.Error}", isWarning: true);
            return remote.IsOk ? remote : local;
        }

        public async Task<Result<bool, RimMindError>> PutAsync(string key, string value)
        {
            var localResult = await _local.PutAsync(key, value);
            var remoteResult = await _remote.PutAsync(key, value);
            if (remoteResult.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote Put failed: {remoteResult.Error}", isWarning: true);
            return localResult;
        }

        public async Task<Result<string?, RimMindError>> GetAsync(string key)
        {
            var local = await _local.GetAsync(key);
            if (local.IsOk && local.Value != null) return local;
            var remote = await _remote.GetAsync(key);
            if (remote.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote Get failed: {remote.Error}", isWarning: true);
            return remote.IsOk ? remote : local;
        }

        public async Task<Result<bool, RimMindError>> DeleteAsync(string key)
        {
            var localResult = await _local.DeleteAsync(key);
            var remoteResult = await _remote.DeleteAsync(key);
            if (remoteResult.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote Delete failed: {remoteResult.Error}", isWarning: true);
            return localResult;
        }

        public async Task<Result<Dictionary<string, string>, RimMindError>> GetBatchAsync(IEnumerable<string> keys)
        {
            var local = await _local.GetBatchAsync(keys);
            if (local.IsOk && local.Value != null && local.Value.Count > 0) return local;
            var remote = await _remote.GetBatchAsync(keys);
            if (remote.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote GetBatch failed: {remote.Error}", isWarning: true);
            return remote.IsOk ? remote : local;
        }

        public async Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json)
        {
            var localResult = await _local.SaveAllEntriesAsync(json);
            var remoteResult = await _remote.SaveAllEntriesAsync(json);
            if (remoteResult.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote SaveAllEntries failed: {remoteResult.Error}", isWarning: true);
            return localResult;
        }

        public async Task<Result<string?, RimMindError>> LoadAllEntriesAsync()
        {
            var local = await _local.LoadAllEntriesAsync();
            if (local.IsOk && local.Value != null) return local;
            var remote = await _remote.LoadAllEntriesAsync();
            if (remote.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote LoadAllEntries failed: {remote.Error}", isWarning: true);
            return remote.IsOk ? remote : local;
        }

        public async Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10)
        {
            var local = await _local.QueryMemoriesAsync(npcId, query, limit);
            if (local.IsOk && local.Value != null && local.Value.Count > 0) return local;
            var remote = await _remote.QueryMemoriesAsync(npcId, query, limit);
            if (remote.IsErr)
                _logSink?.LogFromBackground($"[RimMind-Core] HybridDriver: remote QueryMemories failed: {remote.Error}", isWarning: true);
            return remote.IsOk ? remote : local;
        }
    }
}
