using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts.Result;

namespace RimMind.Contracts.Npc
{
    public interface IStorageDriver
    {
        bool IsRemote { get; }
        bool SupportsStreaming { get; }
        bool SupportsTts { get; }
        bool SupportsCommands { get; }
        bool SupportsStructuredOutput { get; }

        Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null);
        IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, CancellationToken ct = default);
        Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile);
        Task<Result<bool, RimMindError>> KillNpcAsync(string npcId);
        bool IsNpcAlive(string npcId);
        Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json);
        Task<Result<string?, RimMindError>> LoadAllEntriesAsync();
        Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10);
        Task<Result<bool, RimMindError>> PutAsync(string key, string value);
    }
}
