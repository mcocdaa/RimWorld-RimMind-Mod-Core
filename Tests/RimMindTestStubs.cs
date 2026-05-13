using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Domain.Events;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Runtime;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Common.Models.UI;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Prompt;
using RimMind.Application.Features.Queue;
using RimMind.Application.Features.Registry;
using RimMind.Application.Features.Tools;

using IParameterTunerContract = RimMind.Application.Common.Interfaces.Extension.IParameterTuner;
using IStorageDriverApp = RimMind.Application.Common.Interfaces.Npc.IStorageDriver;

namespace RimMind.Application.Features.Context
{
    internal sealed class StubStorageDriver : IStorageDriverApp
    {
        public bool IsRemote => false;
        public bool SupportsStreaming => false;
        public bool SupportsTts => false;
        public bool SupportsCommands => false;
        public bool SupportsStructuredOutput => false;
        public bool IsNpcAlive(string npcId) => false;
        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId) => Task.FromResult(Result<bool, RimMindError>.Ok(true));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(ContextSnapshot snapshot, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string message, string? context = null, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public Task<Result<NpcChatResult, RimMindError>> ChatAsync(string npcId, string sender, string message, string? gameStateInfo = null, CancellationToken ct = default)
            => Task.FromResult(Result<NpcChatResult, RimMindError>.Ok(new NpcChatResult()));
        public async IAsyncEnumerable<Result<NpcChatChunk, RimMindError>> ChatStreamingAsync(string npcId, string senderName, string query, Action<string>? onChunk, string? gameStateInfo = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return Result<NpcChatChunk, RimMindError>.Ok(new NpcChatChunk(npcId, "", isFinal: true));
            await Task.CompletedTask;
        }
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

namespace RimMind.Tests
{
    internal sealed class VerseTickProvider : RimMind.Application.Common.Interfaces.Abstractions.ITickProvider
    {
        public int TicksGame => 0;
    }
}
