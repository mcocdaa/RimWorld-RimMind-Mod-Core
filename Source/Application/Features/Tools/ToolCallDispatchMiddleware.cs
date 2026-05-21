using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Pipeline.AI;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Tools
{
    /// <summary>
    /// Tool call dispatch middleware: the single entry point for dispatching tool calls
    /// from AI responses to registered IToolHandler instances.
    /// </summary>
    internal sealed class ToolCallDispatchMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "ToolCallDispatch";
        public int Order => 600;
        public string Id => "ToolCallDispatch";
        public string OwnerModId => "RimMindCore";

        private readonly IToolRegistry _toolRegistry;
        private readonly ILogSink? _log;
        private readonly int _maxDepth;

        public ToolCallDispatchMiddleware(IToolRegistry toolRegistry, ILogSink? log = null, int maxDepth = 3)
        {
            _toolRegistry = toolRegistry;
            _log = log;
            _maxDepth = maxDepth;
        }

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            await next(context);

            // After the pipeline completes, check if the response contains tool calls
            if (context.Response?.ToolCallsJson == null) return;
            if (context.IsShortCircuited) return;

            var toolCalls = ParseToolCalls(context.Response.ToolCallsJson);
            if (toolCalls == null || toolCalls.Count == 0) return;

            _log?.Message($"[ToolCallDispatch] Dispatching {toolCalls.Count} tool call(s) for request {context.Request.RequestId}");

            var results = new List<ToolResult>();
            foreach (var tc in toolCalls)
            {
                var result = await DispatchToolCallAsync(tc, context, context.Ct);
                results.Add(result);
            }

            // Store results in context for downstream consumers
            context.ToolCallResults = results;

            _log?.Message($"[ToolCallDispatch] Completed {results.Count} tool call(s): " +
                $"{results.Count(r => !r.IsError)} ok, {results.Count(r => r.IsError)} errors");
        }

        private async Task<ToolResult> DispatchToolCallAsync(ToolCallEntry entry, AIRequestContext context, CancellationToken ct)
        {
            var handler = _toolRegistry.FindById(entry.FunctionName);
            if (handler == null)
            {
                _log?.Warning($"[ToolCallDispatch] No handler found for tool: {entry.FunctionName}");
                return ToolResult.Fail($"Unknown tool: {entry.FunctionName}", entry.Id);
            }

            var args = new ToolCallArgs
            {
                ToolCallId = entry.Id,
                ToolName = entry.FunctionName,
                ArgumentsJson = entry.ArgumentsJson,
                NpcId = context.Request.NpcId,
                Ct = ct,
                TraceId = context.TraceId
            };

            var result = await handler.ExecuteAsync(args, ct);
            return result.Match(
                ok => ok,
                err =>
                {
                    _log?.Warning($"[ToolCallDispatch] Tool {entry.FunctionName} failed: {err.Message}");
                    return ToolResult.Fail(err.Message, entry.Id);
                });
        }

        private List<ToolCallEntry>? ParseToolCalls(string json)
        {
            try
            {
                var dtos = JsonConvert.DeserializeObject<List<ToolCallDtoInternal>>(json);
                if (dtos == null) return null;
                return dtos.Select(d => new ToolCallEntry
                {
                    Id = d.id ?? "",
                    Type = d.type ?? "function",
                    FunctionName = d.function?.name ?? "",
                    ArgumentsJson = d.function?.arguments ?? "{}"
                }).ToList();
            }
            catch (JsonException ex)
            {
                _log?.Warning($"[ToolCallDispatch] Failed to parse tool calls JSON: {ex.Message}");
                return null;
            }
        }

        // Internal DTO matching OpenAI tool_call structure
#pragma warning disable CS0649 // Field is never assigned to, JSON deserialization fills these
        private class ToolCallDtoInternal
        {
            [JsonProperty("id")]
            public string? id;
            [JsonProperty("type")]
            public string? type;
            [JsonProperty("function")]
            public ToolCallFunctionDtoInternal? function;
        }

        private class ToolCallFunctionDtoInternal
        {
            [JsonProperty("name")]
            public string? name;
            [JsonProperty("arguments")]
            public string? arguments;
        }
#pragma warning restore CS0649
    }

    /// <summary>
    /// Parsed tool call entry used internally by the dispatch middleware.
    /// </summary>
    internal sealed class ToolCallEntry
    {
        public string Id { get; init; } = "";
        public string Type { get; init; } = "function";
        public string FunctionName { get; init; } = "";
        public string ArgumentsJson { get; init; } = "{}";
    }
}
