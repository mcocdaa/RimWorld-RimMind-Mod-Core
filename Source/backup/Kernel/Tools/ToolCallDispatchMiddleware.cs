using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.AgentBus;
using RimMind.Contracts.Client;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Result;
using RimMind.Contracts.Tools;
using RimMind.Kernel.Json;
using RimMind.Kernel.Pipeline.AI;

namespace RimMind.Kernel.Tools
{
    public sealed class ToolCallDispatchMiddleware : IMiddleware<AIRequestContext>
    {
        private const int DefaultMaxDepth = 3;
        private readonly IToolRegistry _registry;
        private readonly IAgentBus? _bus;
        private readonly Func<int> _getMaxDepth;

        public string Id => "tool_call_dispatch";
        public string Name => "ToolCallDispatch";
        public int Order => 850;

        public ToolCallDispatchMiddleware(IToolRegistry registry, IAgentBus? bus = null, Func<int>? getMaxDepth = null)
        {
            _registry = registry;
            _bus = bus;
            _getMaxDepth = getMaxDepth ?? (() => DefaultMaxDepth);
        }

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            await next(context).ConfigureAwait(false);

            if (context.Result == null) return;
            var result = context.Result.Value;
            if (result.IsErr) return;

            var response = result.Value;
            if (string.IsNullOrEmpty(response.ToolCallsJson)) return;

            int maxDepth = _getMaxDepth();
            int depth = context.Items.TryGetValue("tool_call_depth", out var depthObj) && depthObj is int d ? d : 0;
            var currentResponse = response;

            while (!string.IsNullOrEmpty(currentResponse.ToolCallsJson) && depth < maxDepth)
            {
                depth++;
                context.Items["tool_call_depth"] = depth;

                var toolCalls = JsonHelpers.SafeDeserializeArray<ChatToolCall>(currentResponse.ToolCallsJson);
                if (toolCalls.Length == 0) break;

                var toolMessages = new List<ChatMessage>();

                foreach (var tc in toolCalls)
                {
                    var handler = _registry.FindById(tc.Name);
                    if (handler == null)
                    {
                        var notFoundContent = $"Tool '{tc.Name}' not found";
                        PublishToolResultEvent(context, tc.Name, tc.Id, notFoundContent, true);
                        toolMessages.Add(new ChatMessage { Role = "tool", ToolCallId = tc.Id, Content = notFoundContent });
                        continue;
                    }

                    var args = new ToolCallArgs
                    {
                        ToolId = tc.Name,
                        ToolCallId = tc.Id,
                        ArgumentsJson = tc.Arguments ?? "{}",
                        TraceId = context.TraceId
                    };

                    PublishToolCallEvent(context, tc.Name, tc.Id, args.ArgumentsJson);

                    var sw = Stopwatch.StartNew();
                    string content;
                    bool isError;

                    try
                    {
                        var execResult = await handler.ExecuteAsync(args, context.Ct).ConfigureAwait(false);
                        if (execResult.IsOk)
                        {
                            content = execResult.Value.Content;
                            isError = execResult.Value.IsError;
                        }
                        else
                        {
                            content = execResult.Error.Message;
                            isError = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        content = $"Tool execution failed: {ex.Message}";
                        isError = true;
                    }

                    sw.Stop();
                    PublishToolResultEvent(context, tc.Name, tc.Id, content, isError, sw.Elapsed);
                    toolMessages.Add(new ChatMessage { Role = "tool", ToolCallId = tc.Id, Content = content });
                }

                var request = context.Request;
                if (request.Messages == null)
                    request.Messages = new List<ChatMessage>();

                request.Messages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = currentResponse.Content,
                    ToolCalls = new List<ChatToolCall>(toolCalls)
                });

                foreach (var msg in toolMessages)
                    request.Messages.Add(msg);

                var sendResult = await context.Client!.SendAsync(request).ConfigureAwait(false);
                if (sendResult.IsErr)
                {
                    context.Result = sendResult;
                    return;
                }

                currentResponse = sendResult.Value;
                context.Result = sendResult;
            }

            if (!string.IsNullOrEmpty(currentResponse.ToolCallsJson) && depth >= maxDepth)
            {
                context.Result = Result<AIResponse, RimMindError>.Err(
                    RimMindErrors.ToolMaxDepthExceeded(maxDepth));
            }
        }

        private void PublishToolCallEvent(AIRequestContext context, string toolId, string toolCallId, string argumentsJson)
        {
            if (_bus == null) return;
            try
            {
                var evt = new ToolCallEvent("", 0, toolId, toolCallId, context.TraceId, argumentsJson);
                _bus.Publish(evt);
            }
            catch { }
        }

        private void PublishToolResultEvent(AIRequestContext context, string toolId, string toolCallId, string content, bool isError, TimeSpan? elapsed = null)
        {
            if (_bus == null) return;
            try
            {
                var evt = new ToolResultEvent("", 0, toolCallId, toolId, content, isError);
                _bus.Publish(evt);
            }
            catch { }
        }
    }
}
