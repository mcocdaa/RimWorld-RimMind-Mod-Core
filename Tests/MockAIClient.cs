using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Tests
{
    public class MockAIClient : IAIClient
    {
        private readonly Queue<Func<LlmRequestEnvelope, Result<LlmResponse, RimMindError>>> _responses = new();

        public bool IsLocalEndpoint => true;

        public bool IsConfigured() => true;

        public bool SupportsStreaming => false;

        public bool SupportsNpcServerState => false;

        public MockAIClient EnqueueResponse(string content, string? toolCallsJson = null, int tokens = 100)
        {
            _responses.Enqueue(_ => Result<LlmResponse, RimMindError>.Ok(new LlmResponse
            {
                Content = content,
                TokensUsed = tokens,
                RequestId = Guid.NewGuid().ToString("N").Substring(0, 8),
                State = AIRequestState.Completed,
                ToolCallsJson = toolCallsJson
            }));
            return this;
        }

        public MockAIClient EnqueueError(RimMindError error)
        {
            _responses.Enqueue(_ => Result<LlmResponse, RimMindError>.Err(error));
            return this;
        }

        public MockAIClient EnqueueFunc(Func<LlmRequestEnvelope, Result<LlmResponse, RimMindError>> factory)
        {
            _responses.Enqueue(factory);
            return this;
        }

        public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
        {
            if (_responses.Count == 0)
                return Task.FromResult(Result<LlmResponse, RimMindError>.Err(
                    RimMindErrors.ClientNotConfigured("MockAIClient: no responses queued")));

            var factory = _responses.Dequeue();
            return Task.FromResult(factory(envelope));
        }

        public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct = default)
        {
            throw new NotImplementedException("MockAIClient: SendStreamAsync not implemented");
        }

        public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
        {
            throw new NotImplementedException("MockAIClient: SpawnNpcAsync not implemented");
        }

        public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
        {
            throw new NotImplementedException("MockAIClient: KillNpcAsync not implemented");
        }

        public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(string npcId, string query, int limit)
        {
            throw new NotImplementedException("MockAIClient: QueryNpcMemoriesAsync not implemented");
        }

        public void Dispose() { }
    }
}
