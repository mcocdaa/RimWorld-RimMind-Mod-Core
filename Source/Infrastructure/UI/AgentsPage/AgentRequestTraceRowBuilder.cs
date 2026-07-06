using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RimMind.Application.Common.Models.Debug;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public static class AgentRequestTraceRowBuilder
    {
        public const int DefaultLimit = 8;
        private const int ToolCallSummaryLimit = 3;

        public static IReadOnlyList<AgentRequestTraceRow> BuildRecent(
            IEnumerable<AIRequestTraceEntry>? entries,
            int limit = DefaultLimit)
        {
            if (entries == null || limit <= 0)
                return Empty();

            var rows = entries
                .Where(entry => entry != null)
                .Reverse()
                .Take(limit)
                .Select(BuildRow)
                .ToList();

            return new ReadOnlyCollection<AgentRequestTraceRow>(rows);
        }

        private static AgentRequestTraceRow BuildRow(AIRequestTraceEntry entry)
        {
            return new AgentRequestTraceRow(
                MapStatus(entry.State),
                BuildToolCallSummary(entry),
                FirstNonEmpty(entry.Response, entry.UserPrompt, entry.Source, entry.RequestId),
                ResolveError(entry));
        }

        private static AgentRequestTraceStatus MapStatus(AIRequestTraceState state)
        {
            switch (state)
            {
                case AIRequestTraceState.Completed:
                    return AgentRequestTraceStatus.Success;
                case AIRequestTraceState.Failed:
                    return AgentRequestTraceStatus.Error;
                case AIRequestTraceState.Running:
                default:
                    return AgentRequestTraceStatus.Pending;
            }
        }

        private static string BuildToolCallSummary(AIRequestTraceEntry entry)
        {
            if (entry.ToolCalls.Count == 0)
                return string.Empty;

            return "toolcall: " + string.Join(", ",
                entry.ToolCalls
                    .Take(ToolCallSummaryLimit)
                    .Select(toolCall => toolCall.ToolName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        private static string? ResolveError(AIRequestTraceEntry entry)
        {
            if (entry.State == AIRequestTraceState.Failed && !string.IsNullOrWhiteSpace(entry.Error))
                return entry.Error;

            var failedToolCall = entry.ToolCalls.FirstOrDefault(toolCall =>
                !toolCall.Succeeded && !string.IsNullOrWhiteSpace(toolCall.Error));
            return failedToolCall?.Error;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private static IReadOnlyList<AgentRequestTraceRow> Empty()
            => new ReadOnlyCollection<AgentRequestTraceRow>(Array.Empty<AgentRequestTraceRow>());
    }
}
