using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Debug;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Verse;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class ClientInvokeMiddlewareTests
    {
        private class StubStreamingClient : IAIClient
        {
            public string Id => "stub-streaming";
            public bool IsLocalEndpoint => false;
            public bool IsConfigured() => true;
            public bool SupportsStreaming => true;
            public bool SupportsNpcServerState => false;
            public int SendStreamCallCount { get; private set; }
            public int SendAsyncCallCount { get; private set; }

            private readonly List<LlmChunk> _chunks;

            public StubStreamingClient(List<LlmChunk>? chunks = null)
            {
                _chunks = chunks ?? new List<LlmChunk>();
            }

            public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                SendAsyncCallCount++;
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = envelope.RequestId, Content = "non-streaming-response" }));
            }

            public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct = default)
            {
                SendStreamCallCount++;
                foreach (var chunk in _chunks)
                {
                    onChunk(chunk);
                }
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = envelope.RequestId, Content = "streaming-response", State = AIRequestState.Completed }));
            }

            public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
                => throw new NotImplementedException();
            public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
                => throw new NotImplementedException();
            public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(string npcId, string query, int limit)
                => throw new NotImplementedException();
            public void Dispose() { }
        }

        private class StubNonStreamingClient : IAIClient
        {
            public string Id => "stub-non-streaming";
            public bool IsLocalEndpoint => false;
            public bool IsConfigured() => true;
            public bool SupportsStreaming => false;
            public bool SupportsNpcServerState => false;
            public int SendAsyncCallCount { get; private set; }

            private readonly Result<LlmResponse, RimMindError> _result;

            public StubNonStreamingClient(Result<LlmResponse, RimMindError> result)
            {
                _result = result;
            }

            public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                SendAsyncCallCount++;
                return Task.FromResult(_result);
            }

            public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct = default)
                => throw new NotImplementedException();

            public Task<Result<bool, RimMindError>> SpawnNpcAsync(NpcProfile profile)
                => throw new NotImplementedException();
            public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
                => throw new NotImplementedException();
            public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(string npcId, string query, int limit)
                => throw new NotImplementedException();
            public void Dispose() { }
        }

        private static LlmRequestContext CreateContext(
            IAIClient? client = null,
            bool isStreaming = false)
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    IsStreaming = isStreaming,
                },
                Client = client,
            };
        }

        [Fact]
        public async Task NoClient_ShortCircuitsWithError()
        {
            var middleware = new ClientInvokeMiddleware();
            var context = CreateContext(client: null);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("NoClient", context.ShortCircuitReason);
            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsErr);
        }

        [Fact]
        public async Task NonStreaming_CallsSendAsync()
        {
            var client = new StubNonStreamingClient(
                Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "hello" }));
            var middleware = new ClientInvokeMiddleware();
            var context = CreateContext(client: client, isStreaming: false);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.Equal(1, client.SendAsyncCallCount);
            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsOk);
            Assert.Equal("hello", context.Result.Value.Value.Content);
        }

        [Fact]
        public async Task Streaming_CallsSendStreamAsync()
        {
            var chunks = new List<LlmChunk>
            {
                new LlmChunk { DeltaContent = "Hel" },
                new LlmChunk { DeltaContent = "lo" },
                new LlmChunk { DeltaContent = "!", IsLast = true, FinalResponse = new LlmResponse { RequestId = "req-1", Content = "Hello!" } },
            };
            var client = new StubStreamingClient(chunks);
            var middleware = new ClientInvokeMiddleware();
            var context = CreateContext(client: client, isStreaming: true);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.Equal(1, client.SendStreamCallCount);
            Assert.Equal(0, client.SendAsyncCallCount);
            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsOk);
        }

        [Fact]
        public async Task NonStreaming_ErrorResult_StillSetsResult()
        {
            var client = new StubNonStreamingClient(
                Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient("timeout")));
            var middleware = new ClientInvokeMiddleware();
            var context = CreateContext(client: client, isStreaming: false);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsErr);
        }

        [Fact]
        public async Task NonStreaming_UpdatesTraceWithFinalMessagesSentToClient()
        {
            var client = new StubNonStreamingClient(
                Result<LlmResponse, RimMindError>.Ok(new LlmResponse { RequestId = "req-1", Content = "ok" }));
            var traceLog = new AIRequestTraceLog();
            traceLog.StartRequest("req-1", "raw", "model", "stale", "stale", "");
            var middleware = new ClientInvokeMiddleware(traceLog: traceLog);
            var context = CreateContext(client: client, isStreaming: false);
            context.Envelope.Messages.Add(new ChatMessage { Role = "system", LayerTag = "L0", Content = "final system" });
            context.Envelope.Messages.Add(new ChatMessage { Role = "user", Content = "final user" });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            var entry = Assert.Single(traceLog.Entries);
            Assert.Equal("[L0] final system", entry.SystemPrompt);
            Assert.Equal("final user", entry.UserPrompt);
        }
    }
}
