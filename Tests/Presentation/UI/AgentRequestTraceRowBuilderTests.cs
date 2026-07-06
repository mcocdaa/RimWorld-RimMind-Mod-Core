using System.Collections.Generic;
using RimMind.Application.Common.Models.Debug;
using RimMind.Infrastructure.UI.AgentsPage;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public sealed class AgentRequestTraceRowBuilderTests
    {
        [Fact]
        public void BuildRecent_MapsStatesToAgentTraceStatuses()
        {
            var rows = AgentRequestTraceRowBuilder.BuildRecent(new[]
            {
                Entry("running", AIRequestTraceState.Running),
                Entry("completed", AIRequestTraceState.Completed),
                Entry("failed", AIRequestTraceState.Failed)
            });

            Assert.Equal(AgentRequestTraceStatus.Error, rows[0].Status);
            Assert.Equal(AgentRequestTraceStatus.Success, rows[1].Status);
            Assert.Equal(AgentRequestTraceStatus.Pending, rows[2].Status);
        }

        [Fact]
        public void BuildRecent_UsesRecentLimitNewestFirst()
        {
            var entries = new List<AIRequestTraceEntry>();
            for (int i = 1; i <= 10; i++)
                entries.Add(Entry("request-" + i, AIRequestTraceState.Completed));

            var rows = AgentRequestTraceRowBuilder.BuildRecent(entries, limit: 3);

            Assert.Equal(3, rows.Count);
            Assert.Equal("request-10", rows[0].ContentSummary);
            Assert.Equal("request-9", rows[1].ContentSummary);
            Assert.Equal("request-8", rows[2].ContentSummary);
        }

        [Fact]
        public void BuildRecent_SummarizesFirstFewToolCallNames()
        {
            var entry = Entry("request", AIRequestTraceState.Completed);
            entry.ToolCalls.Add(new AIRequestToolCallTrace("1", "move", true, null));
            entry.ToolCalls.Add(new AIRequestToolCallTrace("2", "wait", true, null));
            entry.ToolCalls.Add(new AIRequestToolCallTrace("3", "equip", true, null));
            entry.ToolCalls.Add(new AIRequestToolCallTrace("4", "ignored", true, null));

            var rows = AgentRequestTraceRowBuilder.BuildRecent(new[] { entry });

            Assert.Equal("toolcall: move, wait, equip", rows[0].ToolCallSummary);
        }

        [Fact]
        public void BuildRecent_ContentSummaryPrefersResponsePromptSourceThenRequestId()
        {
            var rows = AgentRequestTraceRowBuilder.BuildRecent(new[]
            {
                Entry("id-only", AIRequestTraceState.Completed),
                Entry("source", AIRequestTraceState.Completed, source: "source text"),
                Entry("prompt", AIRequestTraceState.Completed, source: "source text", userPrompt: "prompt text"),
                Entry("response", AIRequestTraceState.Completed, source: "source text", userPrompt: "prompt text", response: "response text")
            });

            Assert.Equal("response text", rows[0].ContentSummary);
            Assert.Equal("prompt text", rows[1].ContentSummary);
            Assert.Equal("source text", rows[2].ContentSummary);
            Assert.Equal("id-only", rows[3].ContentSummary);
        }

        [Fact]
        public void BuildRecent_UsesEntryErrorForFailedEntry()
        {
            var rows = AgentRequestTraceRowBuilder.BuildRecent(new[]
            {
                Entry("failed", AIRequestTraceState.Failed, error: "request failed")
            });

            Assert.Equal("request failed", rows[0].Error);
        }

        [Fact]
        public void BuildRecent_UsesFirstFailedToolCallErrorWhenEntryErrorIsMissing()
        {
            var entry = Entry("tool-failed", AIRequestTraceState.Completed);
            entry.ToolCalls.Add(new AIRequestToolCallTrace("1", "move", true, null));
            entry.ToolCalls.Add(new AIRequestToolCallTrace("2", "equip", false, "tool failed"));

            var rows = AgentRequestTraceRowBuilder.BuildRecent(new[] { entry });

            Assert.Equal("tool failed", rows[0].Error);
            Assert.True(rows[0].HasError);
        }

        [Fact]
        public void BuildRecent_MissingEntriesReturnEmptyRows()
        {
            var rows = AgentRequestTraceRowBuilder.BuildRecent(null);

            Assert.Empty(rows);
        }

        private static AIRequestTraceEntry Entry(
            string requestId,
            AIRequestTraceState state,
            string source = "",
            string userPrompt = "",
            string response = "",
            string? error = null)
        {
            return new AIRequestTraceEntry
            {
                RequestId = requestId,
                State = state,
                Source = source,
                UserPrompt = userPrompt,
                Response = response,
                Error = error
            };
        }
    }
}
