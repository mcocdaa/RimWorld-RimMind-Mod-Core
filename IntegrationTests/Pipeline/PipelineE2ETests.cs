using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Application.Features.Tools;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.IntegrationTests.Stubs;

namespace RimMind.IntegrationTests.Pipeline
{
    [Collection("RimWorld Integration")]
    public class PipelineE2ETests : TestBase
    {
        public PipelineE2ETests(TestWorldFixture fixture) : base(fixture) { }

        /// <summary>
        /// Verifies that the full 12-middleware pipeline executes end-to-end
        /// and produces a successful LlmResponse result.
        /// </summary>
        [Fact]
        public async Task FullPipeline_ShouldExecuteMiddlewareChain()
        {
            // Arrange
            var toolRegistry = new ToolRegistry();
            var stubClient = new StubAIClient();
            var pipeline = UnifiedRequestPipelineFactory.Build(toolRegistry);

            var envelope = new LlmRequestEnvelope
            {
                RequestId = "e2e-test-001",
                ScenarioId = "test",
                ModId = "RimMindCore",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Hello" }
                }
            };

            var context = new LlmRequestContext(envelope);
            context.Client = stubClient;

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result!.Value.IsOk.Should().BeTrue();
            context.Result.Value.Value.Should().NotBeNull();
            context.Result.Value.Value!.Content.Should().Be("test response");
            context.Result.Value.Value!.TokensUsed.Should().Be(10);
        }

        /// <summary>
        /// Verifies that when the AI response contains tool_calls JSON,
        /// ToolCallDispatchMiddleware dispatches them and populates ToolCallResults.
        /// </summary>
        [Fact]
        public async Task FullPipeline_WithToolCalls_ShouldDispatch()
        {
            // Arrange
            var toolRegistry = new ToolRegistry();
            var handler = new StubToolHandler("test_tool", "A test tool");
            toolRegistry.Register(handler);

            var stubClient = new StubAIClientWithToolCalls();
            var pipeline = UnifiedRequestPipelineFactory.Build(toolRegistry);

            var envelope = new LlmRequestEnvelope
            {
                RequestId = "e2e-tool-001",
                ScenarioId = "test",
                ModId = "RimMindCore",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Use the test tool" }
                }
            };

            var context = new LlmRequestContext(envelope);
            context.Client = stubClient;

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result!.Value.IsOk.Should().BeTrue();
            context.ToolCallResults.Should().NotBeNull();
            context.ToolCallResults.Should().HaveCount(1);
            context.ToolCallResults![0].ToolName.Should().Be("test_tool");
            context.ToolCallResults[0].IsError.Should().BeFalse();
        }

        /// <summary>
        /// Stub IAIClient that returns a response with tool_calls JSON.
        /// </summary>
        private sealed class StubAIClientWithToolCalls : IAIClient
        {
            public bool IsLocalEndpoint => false;
            public bool IsConfigured() => true;
            public bool SupportsStreaming => false;
            public bool SupportsNpcServerState => false;

            public Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                var toolCallsJson = @"[{""id"":""tc_1"",""type"":""function"",""function"":{""name"":""test_tool"",""arguments"":""{}""}}]";
                var response = new LlmResponse
                {
                    Content = "I will use the test tool",
                    ToolCallsJson = toolCallsJson,
                    TokensUsed = 20
                };
                return Task.FromResult(Result<LlmResponse, RimMindError>.Ok(response));
            }

            public Task<Result<LlmResponse, RimMindError>> SendStreamAsync(
                LlmRequestEnvelope envelope, Action<LlmChunk> onChunk, CancellationToken ct)
            {
                return SendAsync(envelope);
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
        /// Stub IToolHandler for testing tool dispatch.
        /// </summary>
        private sealed class StubToolHandler : IToolHandler
        {
            public string Id => Definition.Id;
            public string OwnerModId => "RimMindCore";
            public ToolDefinition Definition { get; }

            public StubToolHandler(string id, string description)
            {
                Definition = new ToolDefinition
                {
                    Id = id,
                    Description = description,
                    ParametersSchema = "{}",
                    Category = "test"
                };
            }

            public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
            {
                var result = ToolResult.Ok("stub result", args.ToolCallId, args.ToolName);
                return Task.FromResult(Result<ToolResult, RimMindError>.Ok(result));
            }
        }
    }
}
