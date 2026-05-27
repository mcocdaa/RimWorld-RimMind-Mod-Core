using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;

namespace RimMind.IntegrationTests.Stubs
{
    internal sealed class StubAIClient : IAIClient
    {
        public bool IsLocalEndpoint => false;

        public bool IsConfigured() => true;

        public bool SupportsStreaming => false;

        public bool SupportsNpcServerState => false;

        public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
        {
            var response = new AIResponse
            {
                Content = "test response",
                TokensUsed = 10
            };
            return Task.FromResult(Result<AIResponse, RimMindError>.Ok(response));
        }

        public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
        {
            throw new NotImplementedException("K3: LlmRequestEnvelope SendAsync");
        }

        public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct)
        {
            throw new NotImplementedException("K3: SendStreamAsync");
        }

        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            throw new NotImplementedException("K3: SpawnNpcAsync");
        }

        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            throw new NotImplementedException("K3: KillNpcAsync");
        }

        public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(string npcId, string query, int limit)
        {
            throw new NotImplementedException("K3: QueryNpcMemoriesAsync");
        }

        public void Dispose() { }
    }
}
