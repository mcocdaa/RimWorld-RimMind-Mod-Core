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
                entries.SelectMany(entry => entry.ToolCalls.Select(toolCall => ToRow(entry, toolCall))));
        }

        private static DebugTableRow ToRow(AIRequestTraceEntry entry, AIRequestToolCallTrace toolCall)
        {
            string duration = entry.ElapsedMs > 0 ? entry.ElapsedMs + " ms" : string.Empty;
            string summary = toolCall.Succeeded
                ? string.Empty
                : toolCall.Error ?? string.Empty;

            return DebugTableRow.Create(
                toolCall.ToolCallId,
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
