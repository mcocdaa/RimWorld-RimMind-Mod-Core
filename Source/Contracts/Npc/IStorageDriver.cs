using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RimMind.Contracts.Npc
{
    public interface IStorageDriver
    {
        bool IsRemote { get; }
        bool SupportsStreaming { get; }
        bool SupportsTts { get; }
        bool SupportsCommands { get; }
        bool SupportsStructuredOutput { get; }

        Task<NpcChatResult> ChatAsync(string npcId, string message, string? context = null);
        Task<NpcChatResult> ChatStreamingAsync(string npcId, string sender, string message, Action<string>? onChunk, string? gameStateInfo = null, CancellationToken ct = default);
        Task<bool> SpawnNpcAsync(NpcProfile profile);
        Task<bool> KillNpcAsync(string npcId);
        bool IsNpcAlive(string npcId);
        Task<bool> SaveAllEntriesAsync(string json);
        Task<string?> LoadAllEntriesAsync();
        Task<List<string>> QueryMemoriesAsync(string npcId, string query, int limit = 10);
        Task<bool> PutAsync(string key, string value);
    }
}
