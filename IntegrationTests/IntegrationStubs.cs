using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;

using IStorageDriverApp = RimMind.Application.Common.Interfaces.Npc.IStorageDriver;

namespace RimMind.IntegrationTests.Stubs
{
    internal sealed class StubAIClient : IAIClient
    {
        public bool IsLocalEndpoint => false;

        public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
        {
            var response = new AIResponse
            {
                Content = "test response",
                TokensUsed = 10
            };
            return Task.FromResult(Result<AIResponse, RimMindError>.Ok(response));
        }
    }

    internal sealed class StubStorageDriver : IStorageDriverApp
    {
        public bool IsRemote => false;
        public bool SupportsStreaming => false;
        public bool SupportsTts => false;
        public bool SupportsCommands => false;
        public bool SupportsStructuredOutput => false;
        public bool IsNpcAlive(string npcId) => true;
        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<string, RimMindError>> GetHistoryAsync(string npcId, int limit = 50) => Task.FromResult(Result<string, RimMindError>.Ok(""));
        public Task<Result<bool, RimMindError>> PutAsync(string key, string value) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<string?, RimMindError>> GetAsync(string key) => Task.FromResult(Result<string?, RimMindError>.Ok((string?)null));
        public Task<Result<bool, RimMindError>> DeleteAsync(string key) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<Dictionary<string, string>, RimMindError>> GetBatchAsync(IEnumerable<string> keys) => Task.FromResult(Result<Dictionary<string, string>, RimMindError>.Ok(new Dictionary<string, string>()));
        public Task<Result<bool, RimMindError>> SaveAllEntriesAsync(string json) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<string?, RimMindError>> LoadAllEntriesAsync() => Task.FromResult(Result<string?, RimMindError>.Ok((string?)null));
        public Task<Result<List<string>, RimMindError>> QueryMemoriesAsync(string npcId, string query, int limit = 10) => Task.FromResult(Result<List<string>, RimMindError>.Ok(new List<string>()));
    }
}
