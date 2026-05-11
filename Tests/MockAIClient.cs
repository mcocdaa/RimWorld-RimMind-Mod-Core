using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Result;

namespace RimMind.Core.Tests
{
    public class MockAIClient : IAIClient
    {
        private readonly Queue<Func<AIRequest, Result<AIResponse, RimMindError>>> _responses = new();

        public bool IsLocalEndpoint => true;

        public MockAIClient EnqueueResponse(string content, string? toolCallsJson = null, int tokens = 100)
        {
            _responses.Enqueue(_ => Result<AIResponse, RimMindError>.Ok(new AIResponse
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
            _responses.Enqueue(_ => Result<AIResponse, RimMindError>.Err(error));
            return this;
        }

        public MockAIClient EnqueueFunc(Func<AIRequest, Result<AIResponse, RimMindError>> factory)
        {
            _responses.Enqueue(factory);
            return this;
        }

        public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
        {
            if (_responses.Count == 0)
                return Task.FromResult(Result<AIResponse, RimMindError>.Err(
                    RimMindErrors.ClientNotConfigured("MockAIClient: no responses queued")));

            var factory = _responses.Dequeue();
            return Task.FromResult(factory(request));
        }
    }
}
