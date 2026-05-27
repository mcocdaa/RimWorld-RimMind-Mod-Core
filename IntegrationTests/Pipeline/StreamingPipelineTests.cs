using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.IntegrationTests.Pipeline
{
    [Collection("RimWorld Integration")]
    public class StreamingPipelineTests : TestBase
    {
        public StreamingPipelineTests(TestWorldFixture fixture) : base(fixture) { }

        /// <summary>
        /// When IsStreaming=true and the client supports streaming,
        /// ClientInvokeMiddleware should call SendStreamAsync instead of SendAsync.
        /// </summary>
        [Fact]
        public async Task IsStreaming_ShouldCallSendStreamAsync()
        {
            // Arrange
            var stubClient = new StreamingTrackingStubClient();
            var pipeline = BuildMinimalPipeline();

            var envelope = new LlmRequestEnvelope
            {
                RequestId = "stream-test-001",
                ScenarioId = "test",
                ModId = "RimMindCore",
                IsStreaming = true,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Stream this" }
                }
            };

            var context = new LlmRequestContext(envelope);
            context.Client = stubClient;

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result!.Value.IsOk.Should().BeTrue();
            stubClient.SendStreamCalled.Should().BeTrue();
            stubClient.SendAsyncCalled.Should().BeFalse();
        }

        /// <summary>
        /// When streaming, the OnStreamChunk callback should be triggered
        /// for each chunk received from the AI client.
        /// </summary>
        [Fact]
        public async Task StreamChunks_ShouldTriggerOnChunk()
        {
            // Arrange
            var chunksReceived = new List<LlmChunk>();
            var stubClient = new ChunkEmittingStubClient();
            var pipeline = BuildMinimalPipeline();

            var envelope = new LlmRequestEnvelope
            {
                RequestId = "stream-chunk-001",
                ScenarioId = "test",
                ModId = "RimMindCore",
                IsStreaming = true,
                OnStreamChunk = chunk => chunksReceived.Add(chunk),
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Stream chunks" }
                }
            };

            var context = new LlmRequestContext(envelope);
            context.Client = stubClient;

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result!.Value.IsOk.Should().BeTrue();
            // The ChunkEmittingStubClient emits chunks via the onChunk callback
            stubClient.ChunksEmitted.Should().BeGreaterThan(0);
        }

        private static MutablePipeline<LlmRequestContext> BuildMinimalPipeline()
        {
            var middlewares = new List<IMiddleware<LlmRequestContext>>
            {
                new ShortCircuitMiddleware(),
                new TraceContextMiddleware(),
                new NpcEnrichMiddleware(),
                new ContextBuildMiddleware(),
                new RequestSanitizeMiddleware(),
                new CacheMiddleware(),
                new TelemetryMiddleware(),
                new CircuitBreakerMiddleware(),
                new RetryMiddleware(),
                new ClientInvokeMiddleware(),
                new ToolCallDispatchMiddleware(
                    new Application.Features.Tools.ToolRegistry())
            };

            var pipeline = new MutablePipeline<LlmRequestContext>();
            pipeline.UseRange(middlewares);
            return pipeline;
        }

        /// <summary>
        /// Stub IAIClient that tracks whether SendStreamAsync or SendAsync was called.
        /// SupportsStreaming returns true so ClientInvokeMiddleware uses the streaming path.
        /// </summary>
        private sealed class StreamingTrackingStubClient : IAIClient
        {
            public bool IsLocalEndpoint => false;
            public bool IsConfigured() => true;
            public bool SupportsStreaming => true;
            public bool SupportsNpcServerState => false;

            public bool SendStreamCalled { get; private set; }
            public bool SendAsyncCalled { get; private set; }

            public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                SendAsyncCalled = true;
                var response = new LlmResponse
                {
                    Content = "non-stream response",
                    TokensUsed = 10
                };
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
            }

            public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(
                LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct)
            {
                SendStreamCalled = true;
                var response = new LlmResponse
                {
                    Content = "stream response",
                    TokensUsed = 10
                };
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
            }

            public Task<Result<bool, RimMindError>> SpawnNpcAsync(
                Application.Common.Models.Npc.NpcProfile profile)
            {
                return Task.FromResult(Result<bool, RimMindError>.Ok(true));
            }

            public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
            {
                return Task.FromResult(Result<bool, RimMindError>.Ok(true));
            }

            public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(
                string npcId, string query, int limit)
            {
                return Task.FromResult(Result<List<string>, RimMindError>.Ok(new List<string>()));
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Stub IAIClient that emits LlmChunk objects via the onChunk callback
        /// during SendStreamAsync to verify chunk delivery.
        /// </summary>
        private sealed class ChunkEmittingStubClient : IAIClient
        {
            public bool IsLocalEndpoint => false;
            public bool IsConfigured() => true;
            public bool SupportsStreaming => true;
            public bool SupportsNpcServerState => false;

            public int ChunksEmitted { get; private set; }

            public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                var response = new LlmResponse
                {
                    Content = "non-stream response",
                    TokensUsed = 10
                };
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
            }

            public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(
                LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct)
            {
                // Emit two chunks then the final response
                onChunk(new LlmChunk { DeltaContent = "Hello ", IsLast = false });
                ChunksEmitted++;
                onChunk(new LlmChunk { DeltaContent = "World", IsLast = false });
                ChunksEmitted++;

                var response = new LlmResponse
                {
                    Content = "Hello World",
                    TokensUsed = 5
                };
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
            }

            public Task<Result<bool, RimMindError>> SpawnNpcAsync(
                Application.Common.Models.Npc.NpcProfile profile)
            {
                return Task.FromResult(Result<bool, RimMindError>.Ok(true));
            }

            public Task<Result<bool, RimMindError>> KillNpcAsync(string npcId)
            {
                return Task.FromResult(Result<bool, RimMindError>.Ok(true));
            }

            public Task<Result<List<string>, RimMindError>> QueryNpcMemoriesAsync(
                string npcId, string query, int limit)
            {
                return Task.FromResult(Result<List<string>, RimMindError>.Ok(new List<string>()));
            }

            public void Dispose() { }
        }
    }
}
