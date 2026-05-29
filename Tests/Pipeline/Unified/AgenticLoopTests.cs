using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    /// <summary>
    /// Tests for the Agentic Loop closure (N_Tasks_1).
    /// Covers: AgentDecision extensions, ToolCallResultFormatter,
    /// ToolCallDispatchMiddleware ToolCallResults flow, ParseDecision with toolCallResults,
    /// and depth control.
    /// </summary>
    public class AgenticLoopTests
    {
        // === AgentDecision extensions ===

        [Fact]
        public void AgentDecision_DefaultValues_NewFieldsAreDefault()
        {
            var decision = new AgentDecision();
            Assert.Null(decision.ToolCallId);
            Assert.False(decision.WantsMoreToolCalls);
            Assert.Equal(0, decision.ToolCallRound);
        }

        [Fact]
        public void AgentDecision_WithToolCallFields_SetCorrectly()
        {
            var decision = new AgentDecision(
                ActionIntent: "investigate",
                Reason: "need more info",
                ToolCallId: "tc-1",
                WantsMoreToolCalls: true,
                ToolCallRound: 1);
            Assert.Equal("investigate", decision.ActionIntent);
            Assert.Equal("tc-1", decision.ToolCallId);
            Assert.True(decision.WantsMoreToolCalls);
            Assert.Equal(1, decision.ToolCallRound);
        }

        [Fact]
        public void AgentDecision_WithExpression()
        {
            var baseDecision = new AgentDecision(ActionIntent: "eat", Reason: "hungry");
            var extended = baseDecision with
            {
                WantsMoreToolCalls = true,
                ToolCallRound = 2,
                ToolCallId = "tc-2"
            };
            Assert.Equal("eat", extended.ActionIntent);
            Assert.True(extended.WantsMoreToolCalls);
            Assert.Equal(2, extended.ToolCallRound);
            Assert.Equal("tc-2", extended.ToolCallId);
        }

        // === ToolCallResultFormatter ===

        [Fact]
        public void Format_NullResults_ReturnsEmpty()
        {
            var result = ToolCallResultFormatter.Format(null!, 1);
            Assert.Equal("", result);
        }

        [Fact]
        public void Format_EmptyResults_ReturnsEmpty()
        {
            var result = ToolCallResultFormatter.Format(new List<ToolResult>(), 1);
            Assert.Equal("", result);
        }

        [Fact]
        public void Format_SingleSuccessResult_FormatsCorrectly()
        {
            var results = new List<ToolResult>
            {
                ToolResult.Ok("sunny", "tc-1", "get_weather")
            };
            var formatted = ToolCallResultFormatter.Format(results, 1);
            Assert.Contains("[ToolCall Results (Round 1)]", formatted);
            Assert.Contains("- Tool: get_weather -> Result: sunny", formatted);
        }

        [Fact]
        public void Format_SingleErrorResult_FormatsCorrectly()
        {
            var results = new List<ToolResult>
            {
                ToolResult.Fail("timeout", "tc-2", "search_web")
            };
            var formatted = ToolCallResultFormatter.Format(results, 2);
            Assert.Contains("[ToolCall Results (Round 2)]", formatted);
            Assert.Contains("- Tool: search_web -> Error: timeout", formatted);
        }

        [Fact]
        public void Format_MixedResults_FormatsAll()
        {
            var results = new List<ToolResult>
            {
                ToolResult.Ok("sunny", "tc-1", "get_weather"),
                ToolResult.Fail("not found", "tc-2", "search_item")
            };
            var formatted = ToolCallResultFormatter.Format(results, 1);
            Assert.Contains("- Tool: get_weather -> Result: sunny", formatted);
            Assert.Contains("- Tool: search_item -> Error: not found", formatted);
        }

        [Fact]
        public void Format_UnknownToolName_UsesUnknown()
        {
            var results = new List<ToolResult>
            {
                new ToolResult { Content = "data", IsError = false, ToolCallId = "tc-1" }
            };
            var formatted = ToolCallResultFormatter.Format(results, 1);
            Assert.Contains("- Tool: unknown -> Result: data", formatted);
        }

        // === ToolCallDispatchMiddleware ToolCallResults flow ===

        [Fact]
        public async Task Middleware_StoresToolCallResults_InStrongTypedProperty()
        {
            var handler = new StubToolHandler3("get_weather",
                Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = "tc-1", Content = "rainy" }));
            var registry = new StubToolRegistry3();
            registry.Register(handler);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            var toolCallsJson = MakeToolCallsJson(("tc-1", "get_weather", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse
                    {
                        RequestId = "req-1",
                        Content = "checking",
                        ToolCallsJson = toolCallsJson,
                    });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.ToolCallResults);
            Assert.Single(context.ToolCallResults);
            Assert.Equal("rainy", context.ToolCallResults[0].Content);
            Assert.Equal("get_weather", context.ToolCallResults[0].ToolName);
        }

        [Fact]
        public async Task Middleware_NoToolCalls_ToolCallResultsIsNull()
        {
            var registry = new StubToolRegistry3();
            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "no tools" });
                return Task.CompletedTask;
            });

            Assert.Null(context.ToolCallResults);
        }

        [Fact]
        public async Task Middleware_ToolNamePropagatedToResults()
        {
            var handler = new StubToolHandler3("search_web",
                Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = "tc-1", Content = "found" }));
            var registry = new StubToolRegistry3();
            registry.Register(handler);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            var toolCallsJson = MakeToolCallsJson(("tc-1", "search_web", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse
                    {
                        RequestId = "req-1",
                        Content = "searching",
                        ToolCallsJson = toolCallsJson,
                    });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.ToolCallResults);
            Assert.Equal("search_web", context.ToolCallResults[0].ToolName);
        }

        [Fact]
        public async Task Middleware_UnknownTool_ToolNameInFailResult()
        {
            var registry = new StubToolRegistry3();
            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            var toolCallsJson = MakeToolCallsJson(("tc-1", "unknown_tool", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse
                    {
                        RequestId = "req-1",
                        Content = "trying",
                        ToolCallsJson = toolCallsJson,
                    });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.ToolCallResults);
            Assert.Single(context.ToolCallResults);
            Assert.True(context.ToolCallResults[0].IsError);
            Assert.Equal("unknown_tool", context.ToolCallResults[0].ToolName);
        }

        // === ParseDecision with toolCallResults ===

        [Fact]
        public void ParseDecision_WithNullToolCallResults_WorksAsBefore()
        {
            var strategy = new ReactiveThinkStrategy();
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\":\"eat\",\"reason\":\"hungry\"}</Action>"
            };
            var agent = new StubAgentInfo();

            var result = strategy.ParseDecision(agent, response, null);
            Assert.True(result.IsOk);
            Assert.Equal("eat", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecision_WithToolCallResults_PassesThrough()
        {
            var strategy = new ReactiveThinkStrategy();
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\":\"investigate\",\"reason\":\"checking results\"}</Action>"
            };
            var agent = new StubAgentInfo();
            var toolResults = new List<ToolResult>
            {
                ToolResult.Ok("sunny", "tc-1", "get_weather")
            };

            var result = strategy.ParseDecision(agent, response, toolResults);
            Assert.True(result.IsOk);
            Assert.Equal("investigate", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecision_NoActionTag_ReturnsDialogueFree()
        {
            var strategy = new ReactiveThinkStrategy();
            var response = new LlmResponse { Content = "no action here" };
            var agent = new StubAgentInfo();

            var result = strategy.ParseDecision(agent, response,
                new List<ToolResult> { ToolResult.Ok("data", "tc-1") });
            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
        }

        // === Depth control ===

        [Fact]
        public void DefaultMaxToolCallDepth_IsThree()
        {
            Assert.Equal(3, RimMindDefaults.DefaultMaxToolCallDepth);
        }

        [Fact]
        public void LlmRequestContext_ToolCallRound_DefaultIsZero()
        {
            var ctx = new LlmRequestContext();
            Assert.Equal(0, ctx.ToolCallRound);
        }

        [Fact]
        public void LlmRequestContext_ToolCallResults_DefaultIsNull()
        {
            var ctx = new LlmRequestContext();
            Assert.Null(ctx.ToolCallResults);
        }

        // === ToolResult ToolName ===

        [Fact]
        public void ToolResult_Ok_WithToolName()
        {
            var result = ToolResult.Ok("content", "tc-1", "my_tool");
            Assert.Equal("content", result.Content);
            Assert.False(result.IsError);
            Assert.Equal("tc-1", result.ToolCallId);
            Assert.Equal("my_tool", result.ToolName);
        }

        [Fact]
        public void ToolResult_Fail_WithToolName()
        {
            var result = ToolResult.Fail("error msg", "tc-2", "bad_tool");
            Assert.Equal("error msg", result.Content);
            Assert.True(result.IsError);
            Assert.Equal("tc-2", result.ToolCallId);
            Assert.Equal("bad_tool", result.ToolName);
        }

        [Fact]
        public void ToolResult_Ok_BackwardCompatible_NoToolName()
        {
            var result = ToolResult.Ok("content", "tc-1");
            Assert.Equal("content", result.Content);
            Assert.Null(result.ToolName);
        }

        // === Helpers ===

        private static LlmRequestContext CreateContext()
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                },
            };
        }

        private static string MakeToolCallsJson(params (string id, string name, string args)[] calls)
        {
            var items = new List<string>();
            foreach (var (id, name, args) in calls)
            {
                items.Add($"{{\"id\":\"{id}\",\"type\":\"function\",\"function\":{{\"name\":\"{name}\",\"arguments\":\"{args}\"}}}}");
            }
            return $"[{string.Join(",", items)}]";
        }
    }

    // Stubs for agentic loop tests
    internal sealed class StubToolHandler3 : IToolHandler
    {
        public string Id => Definition.Id;
        public string OwnerModId => "Test";
        public ToolDefinition Definition { get; }
        private readonly Result<ToolResult, RimMindError> _result;

        public StubToolHandler3(string id, Result<ToolResult, RimMindError> result)
        {
            Definition = new ToolDefinition { Id = id };
            _result = result;
        }

        public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
            => Task.FromResult(_result);
    }

    internal sealed class StubToolRegistry3 : IToolRegistry
    {
        private readonly Dictionary<string, IToolHandler> _handlers = new();

        public void Register(IToolHandler handler)
        {
            if (handler != null) _handlers[handler.Id] = handler;
        }

        public bool Unregister(string toolId) => _handlers.Remove(toolId);
        public IToolHandler? FindById(string toolId) => _handlers.TryGetValue(toolId, out var h) ? h : null;
        public IReadOnlyList<IToolHandler> All => new List<IToolHandler>(_handlers.Values).AsReadOnly();
        public IReadOnlyList<ToolDefinition> GetAllDefinitions() => new List<ToolDefinition>().AsReadOnly();
    }

    internal sealed class StubAgentInfo : IAgentInfo
    {
        public string NpcId => "test-npc";
        public string Label => "TestPawn";
        public AgentState State => AgentState.Active;
        public int? LastThinkTick { get; set; } = null;
        public int GoalCount => 0;
        public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) => Array.Empty<BehaviorRecordDto>();
        public float GetRecentSuccessRate(int count = 10) => 1.0f;
    }
}
