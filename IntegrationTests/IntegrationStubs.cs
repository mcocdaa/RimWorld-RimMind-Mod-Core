using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Npc;

namespace RimMind.IntegrationTests.Stubs
{
    internal sealed class StubAIClient : IAIClient
    {
        public bool IsLocalEndpoint => false;

        public bool IsConfigured() => true;

        public bool SupportsStreaming => false;

        public bool SupportsNpcServerState => false;

        public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
        {
            var response = new LlmResponse
            {
                Content = "test response",
                TokensUsed = 10
            };
            return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
        }

        public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct)
        {
            var response = new LlmResponse
            {
                Content = "test stream response",
                TokensUsed = 10
            };
            return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
        }

        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            return Task.FromResult(Result<bool, RimMindError>.Ok(true));
        }

        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            return Task.FromResult(Result<bool, RimMindError>.Ok(true));
        }

        public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(string npcId, string query, int limit)
        {
            return Task.FromResult(Result<List<string>, RimMindError>.Ok(new List<string>()));
        }

        public void Dispose() { }
    }
}
