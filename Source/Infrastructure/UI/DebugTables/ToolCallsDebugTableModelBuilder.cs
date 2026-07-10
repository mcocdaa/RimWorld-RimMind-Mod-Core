using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Debug;
using Verse;

namespace RimMind.Infrastructure.UI.DebugTables
{
    public sealed class ToolCallsDebugTableModelBuilder : IDebugTableModelBuilder
    {
        private readonly IAIRequestTraceLog? _log;

        public ToolCallsDebugTableModelBuilder(IAIRequestTraceLog? log)
        {
            _log = log;
        }

        public DebugTableModel Build()
            => Build(_log?.Entries ?? System.Array.Empty<AIRequestTraceEntry>());

        public static DebugTableModel Build(IReadOnlyList<AIRequestTraceEntry> entries)
        {
            return new DebugTableModel(
                "RimMind.UI.Hub.Tab.ToolCalls".Translate(),
                entries.SelectMany(entry => entry.ToolCalls.Select((toolCall, index) => ToRow(entry, toolCall, index))));
        }

        private static DebugTableRow ToRow(AIRequestTraceEntry entry, AIRequestToolCallTrace toolCall, int index)
        {
            string duration = entry.ElapsedMs > 0 ? entry.ElapsedMs + " ms" : string.Empty;
            string summary = toolCall.Succeeded
                ? string.Empty
                : toolCall.Error ?? string.Empty;
            string id = string.IsNullOrWhiteSpace(toolCall.ToolCallId)
                ? entry.RequestId + ":tool:" + index
                : toolCall.ToolCallId;

            return DebugTableRow.Create(
                id,
                toolCall.Succeeded ? DebugTableStatus.Completed : DebugTableStatus.Failed,
                string.Empty,
                entry.Source,
                string.Empty,
                toolCall.ToolName,
                entry.Model,
                summary,
                duration);
        }
    }
}
