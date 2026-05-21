using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Features.Pipeline.AI;
using RimMind.Application.Features.Tools;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class ToolCallDispatchMiddlewareTests
    {
        private class StubToolHandler : IToolHandler
        {
            public string Id => Definition.Id;
            public string OwnerModId => "Test";
            public ToolDefinition Definition { get; }
            private readonly string _resultContent;
            private readonly bool _returnError;

            public StubToolHandler(string id, string resultContent = "tool-result", bool returnError = false)
            {
                Definition = new ToolDefinition { Id = id };
                _resultContent = resultContent;
                _returnError = returnError;
            }

            public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
            {
                if (_returnError)
                {
                    return Task.FromResult(Result<ToolResult, RimMindError>.Err(
                        RimMindErrors.ToolExecution(args.ToolId, "execution failed")));
                }
                return Task.FromResult(Result<ToolResult, RimMindError>.Ok(
                    new ToolResult { ToolCallId = args.ToolCallId, Content = _resultContent }));
            }
        }

        private class StubAIClient : IAIClient
        {
            public string Id => "stub";
            public bool IsLocalEndpoint => false;
            private readonly AIResponse _response;
            private readonly bool _returnError;
            public int SendCount { get; private set; }

            public StubAIClient(AIResponse response, bool returnError = false)
            {
                _response = response;
                _returnError = returnError;
            }

            public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
            {
                SendCount++;
                if (_returnError)
                {
                    return Task.FromResult(Result<AIResponse, RimMindError>.Err(
                        RimMindErrors.ClientTransient("send failed")));
                }
                return Task.FromResult(Result<AIResponse, RimMindError>.Ok(_response));
            }
        }

        private class StubToolRegistry : IToolRegistry
        {
            private readonly Dictionary<string, IToolHandler> _handlers = new();

            public void Register(IToolHandler handler)
            {
                if (handler != null) _handlers[handler.Id] = handler;
            }

            public bool Unregister(string toolId) => _handlers.Remove(toolId);

            public IToolHandler? FindById(string toolId) =>
                _handlers.TryGetValue(toolId, out var h) ? h : null;

            public IReadOnlyList<IToolHandler> All =>
                new List<IToolHandler>(_handlers.Values).AsReadOnly();

            public IReadOnlyList<ToolDefinition> GetAllDefinitions() =>
                new List<ToolDefinition>().AsReadOnly();
        }

        private static AIRequestContext CreateContext(
            IAIClient? client = null,
            AIRequest? request = null)
        {
            return new AIRequestContext
            {
                Request = request ?? new AIRequest
                {
                    RequestId = Guid.NewGuid().ToString("N").Substring(0, 8),
                    ModId = "test",
                    Messages = new List<ChatMessage>(),
                },
                Client = client,
            };
        }

        private static string MakeToolCallsJson(params (string id, string name, string args)[] calls)
        {
            var items = new List<string>();
            foreach (var (id, name, args) in calls)
            {
                items.Add($"{{\"Id\":\"{id}\",\"Name\":\"{name}\",\"Arguments\":\"{args}\"}}");
            }
            return $"[{string.Join(",", items)}]";
        }

        [Fact]
        public void Id_Is_tool_call_dispatch()
        {
            var registry = new StubToolRegistry();
            var middleware = new ToolCallDispatchMiddleware(registry);

            Assert.Equal("tool_call_dispatch", middleware.Id);
        }

        [Fact]
        public void Order_Is_850()
        {
            var registry = new StubToolRegistry();
            var middleware = new ToolCallDispatchMiddleware(registry);

            Assert.Equal(850, middleware.Order);
        }

        [Fact]
        public async Task NoToolCallsJson_ContextResultUnchanged()
        {
            var registry = new StubToolRegistry();
            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext();
            var originalResponse = AIResponse.Ok("req-1", "hello", 10);

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(originalResponse);
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsOk);
            Assert.Equal("hello", context.Result.Value.Value.Content);
        }

        [Fact]
        public async Task WithToolCalls_ExecutesHandlerAndSendsFollowUp()
        {
            var handler = new StubToolHandler("get_weather", "sunny");
            var registry = new StubToolRegistry();
            registry.Register(handler);

            var followUpResponse = AIResponse.Ok("req-2", "The weather is sunny", 20);
            var client = new StubAIClient(followUpResponse);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext(client: client);

            var toolCallsJson = MakeToolCallsJson(("tc-1", "get_weather", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(new AIResponse
                {
                    Content = "let me check",
                    ToolCallsJson = toolCallsJson,
                    RequestId = "req-1",
                });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsOk);
            Assert.Equal("The weather is sunny", context.Result.Value.Value.Content);
            Assert.Equal(1, client.SendCount);

            Assert.NotNull(context.Request.Messages);
            Assert.Equal(2, context.Request.Messages!.Count);
            Assert.Equal("assistant", context.Request.Messages[0].Role);
            Assert.NotNull(context.Request.Messages[0].ToolCalls);
            Assert.Equal("tool", context.Request.Messages[1].Role);
            Assert.Equal("sunny", context.Request.Messages[1].Content);
        }

        [Fact]
        public async Task ToolNotFound_AddsNotFoundMessage()
        {
            var registry = new StubToolRegistry();
            var followUpResponse = AIResponse.Ok("req-2", "done", 5);
            var client = new StubAIClient(followUpResponse);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext(client: client);

            var toolCallsJson = MakeToolCallsJson(("tc-1", "unknown_tool", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(new AIResponse
                {
                    Content = "trying",
                    ToolCallsJson = toolCallsJson,
                    RequestId = "req-1",
                });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Request.Messages);
            var toolMsg = context.Request.Messages!.Find(m => m.Role == "tool");
            Assert.NotNull(toolMsg);
            Assert.Contains("unknown_tool", toolMsg!.Content);
            Assert.Contains("not found", toolMsg.Content);
        }

        [Fact]
        public async Task HandlerReturnsError_UsesErrorMessageAsContent()
        {
            var handler = new StubToolHandler("failing_tool", returnError: true);
            var registry = new StubToolRegistry();
            registry.Register(handler);

            var followUpResponse = AIResponse.Ok("req-2", "handled", 5);
            var client = new StubAIClient(followUpResponse);

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext(client: client);

            var toolCallsJson = MakeToolCallsJson(("tc-1", "failing_tool", "{}"));

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(new AIResponse
                {
                    Content = "calling",
                    ToolCallsJson = toolCallsJson,
                    RequestId = "req-1",
                });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Request.Messages);
            var toolMsg = context.Request.Messages!.Find(m => m.Role == "tool");
            Assert.NotNull(toolMsg);
            Assert.Contains("execution failed", toolMsg!.Content);
        }

        [Fact]
        public async Task MaxDepthExceeded_ReturnsError()
        {
            var handler = new StubToolHandler("loop_tool", "looping");
            var registry = new StubToolRegistry();
            registry.Register(handler);

            var loopingResponse = new AIResponse
            {
                Content = "loop",
                ToolCallsJson = MakeToolCallsJson(("tc-loop", "loop_tool", "{}")),
                RequestId = "req-loop",
            };
            var client = new StubAIClient(loopingResponse);

            var middleware = new ToolCallDispatchMiddleware(registry, bus: null, getMaxDepth: () => 3);
            var context = CreateContext(client: client);

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(new AIResponse
                {
                    Content = "start",
                    ToolCallsJson = MakeToolCallsJson(("tc-1", "loop_tool", "{}")),
                    RequestId = "req-1",
                });
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsErr);
            Assert.Equal(RimMindErrorCode.ToolMaxDepthExceeded, context.Result.Value.Error.Code);
        }

        [Fact]
        public async Task NextMiddlewareSetsErrorResult_DoesNotProcessToolCalls()
        {
            var handler = new StubToolHandler("some_tool", "result");
            var registry = new StubToolRegistry();
            registry.Register(handler);

            var client = new StubAIClient(AIResponse.Ok("req-2", "done", 5));

            var middleware = new ToolCallDispatchMiddleware(registry);
            var context = CreateContext(client: client);

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Err(
                    RimMindErrors.ClientPermanent("api key invalid"));
                return Task.CompletedTask;
            });

            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsErr);
            Assert.Equal(RimMindErrorCode.ClientPermanentFailure, context.Result.Value.Error.Code);
            Assert.Equal(0, client.SendCount);
        }

        [Fact]
        public async Task CustomMaxDepth_Respected()
        {
            var handler = new StubToolHandler("loop_tool", "looping");
            var registry = new StubToolRegistry();
            registry.Register(handler);

            var loopingResponse = new AIResponse
            {
                Content = "loop",
                ToolCallsJson = MakeToolCallsJson(("tc-loop", "loop_tool", "{}")),
                RequestId = "req-loop",
            };
            var client = new StubAIClient(loopingResponse);

            var middleware = new ToolCallDispatchMiddleware(registry, bus: null, getMaxDepth: () => 5);
            var context = CreateContext(client: client);

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<AIResponse, RimMindError>.Ok(new AIResponse
                {
                    Content = "start",
                    ToolCallsJson = MakeToolCallsJson(("tc-1", "loop_tool", "{}")),
                    RequestId = "req-1",
                });
                return Task.CompletedTask;
            });

            Assert.Equal(5, client.SendCount);
        }
    }
}
