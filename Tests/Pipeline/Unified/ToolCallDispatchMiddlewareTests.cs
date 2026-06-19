using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Debug;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Infrastructure.Verse;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubToolHandler2 : IToolHandler
    {
        public string Id => Definition.Id;
        public string OwnerModId => "Test";
        public ToolDefinition Definition { get; }
        private readonly Result<ToolResult, RimMindError> _result;

        public StubToolHandler2(string id, Result<ToolResult, RimMindError> result)
        {
            Definition = new ToolDefinition { Id = id };
            _result = result;
        }

        public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
            => Task.FromResult(_result);
    }

    internal sealed class StubToolRegistry2 : IToolRegistry
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
        public IReadOnlyList<IToolHandler> GetHandlersForScope(AgentScopeKind scopeKind) => All;
        public IReadOnlyList<ToolDefinition> GetDefinitionsForScope(AgentScopeKind scopeKind) => GetAllDefinitions();
    }

    public class ToolCallDispatchMiddlewareTests
    {
        private static LlmRequestContext CreateContext(
            ToolCallDispatchMode toolDispatchMode = ToolCallDispatchMode.Auto)
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    ToolDispatchMode = toolDispatchMode,
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

        [Fact]
        public async Task NoToolCallsJson_DoesNotDispatch()
        {
            var registry = new StubToolRegistry2();
            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "no tools" });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Result);
            Assert.True(context.Result.Value.IsOk);
            Assert.Equal("no tools", context.Result.Value.Value.Content);
        }

        [Fact]
        public async Task WithToolCalls_DispatchesAndStoresResults()
        {
            var handler = new StubToolHandler2("get_weather",
                Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = "tc-1", Content = "sunny" }));
            var registry = new StubToolRegistry2();
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
            Assert.Equal("sunny", context.ToolCallResults[0].Content);
        }

        [Fact]
        public async Task WithToolCalls_RecordsTraceEntries()
        {
            var handler = new StubToolHandler2("get_weather",
                Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = "tc-1", Content = "sunny" }));
            var registry = new StubToolRegistry2();
            registry.Register(handler);
            var traceLog = new AIRequestTraceLog();
            traceLog.StartRequest("req-1", "test", "model", "", "prompt", "");

            var middleware = new ToolCallDispatchMiddleware(registry, traceLog: traceLog);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse
                    {
                        RequestId = "req-1",
                        Content = "checking",
                        ToolCallsJson = MakeToolCallsJson(("tc-1", "get_weather", "{}")),
                    });
                return Task.CompletedTask;
            });

            var entry = Assert.Single(traceLog.Entries);
            var tool = Assert.Single(entry.ToolCalls);
            Assert.Equal("tc-1", tool.ToolCallId);
            Assert.Equal("get_weather", tool.ToolName);
            Assert.True(tool.Succeeded);
            Assert.Null(tool.Error);
        }

        [Fact]
        public async Task ManualDispatchMode_DoesNotExecuteToolCalls()
        {
            var handler = new StubToolHandler2("get_weather",
                Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = "tc-1", Content = "sunny" }));
            var registry = new StubToolRegistry2();
            registry.Register(handler);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext(ToolCallDispatchMode.Manual);

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

            Assert.Null(context.ToolCallResults);
            Assert.Equal(toolCallsJson, context.Result!.Value.Value.ToolCallsJson);
        }

        [Fact]
        public async Task ErrorResult_DoesNotDispatch()
        {
            var registry = new StubToolRegistry2();
            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient("fail"));
                return Task.CompletedTask;
            });

            Assert.Null(context.ToolCallResults);
        }
    }
}
