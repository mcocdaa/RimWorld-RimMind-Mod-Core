using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Debug;
using RimMind.Infrastructure.Verse;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public class P7E_RequestTraceLogTests
    {
        [Fact]
        public void StartRequest_Creates_Running_Entry()
        {
            var log = new AIRequestTraceLog();

            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");

            var entry = Assert.Single(log.Entries);
            Assert.Equal("req-1", entry.RequestId);
            Assert.Equal(AIRequestTraceState.Running, entry.State);
            Assert.Equal("pawn:42", entry.Source);
        }

        [Fact]
        public void CompleteRequest_Updates_Same_Entry()
        {
            var log = new AIRequestTraceLog();

            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");
            log.CompleteRequest("req-1", "ok", tokensUsed: 12, elapsedMs: 30);

            var entry = Assert.Single(log.Entries);
            Assert.Equal(AIRequestTraceState.Completed, entry.State);
            Assert.Equal("ok", entry.Response);
            Assert.Equal(12, entry.TokensUsed);
        }

        [Fact]
        public void FailRequest_Stores_Error_On_Same_Entry()
        {
            var log = new AIRequestTraceLog();

            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");
            log.FailRequest("req-1", "missing api key");

            var entry = Assert.Single(log.Entries);
            Assert.Equal(AIRequestTraceState.Failed, entry.State);
            Assert.Equal("missing api key", entry.Error);
        }

        [Fact]
        public void AddToolCall_Attaches_To_Request()
        {
            var log = new AIRequestTraceLog();

            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");
            log.AddToolCall("req-1", "tool-1", "pawn.job.set", succeeded: true, error: null);

            var entry = Assert.Single(log.Entries);
            var tool = Assert.Single(entry.ToolCalls);
            Assert.Equal("pawn.job.set", tool.ToolName);
            Assert.True(tool.Succeeded);
        }

        [Fact]
        public void Clear_Removes_All_Entries()
        {
            var log = new AIRequestTraceLog();
            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");
            log.StartRequest("req-2", "pawn:43", "deepseek-chat", "world");

            log.Clear();

            Assert.Empty(log.Entries);
        }

        [Fact]
        public void CompleteRequest_Without_Start_Creates_Entry()
        {
            var log = new AIRequestTraceLog();

            log.CompleteRequest("req-orphan", "ok", tokensUsed: 5, elapsedMs: 10);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("req-orphan", entry.RequestId);
            Assert.Equal(AIRequestTraceState.Completed, entry.State);
            Assert.Equal("ok", entry.Response);
        }

        [Fact]
        public void StartRequest_Duplicate_Resets_Existing_Entry()
        {
            var log = new AIRequestTraceLog();
            log.StartRequest("req-1", "pawn:42", "deepseek-chat", "hello");
            log.CompleteRequest("req-1", "ok", tokensUsed: 5, elapsedMs: 10);
            log.AddToolCall("req-1", "tool-1", "pawn.job.set", succeeded: true, error: null);

            log.StartRequest("req-1", "pawn:99", "gpt-4", "restart");

            var entry = Assert.Single(log.Entries);
            Assert.Equal(AIRequestTraceState.Running, entry.State);
            Assert.Equal("pawn:99", entry.Source);
            Assert.Equal("gpt-4", entry.Model);
            Assert.Equal("restart", entry.UserPrompt);
            Assert.Empty(entry.ToolCalls);
        }

        [Fact]
        public void AddToolCall_Without_Start_Creates_Entry()
        {
            var log = new AIRequestTraceLog();

            log.AddToolCall("req-orphan", "tool-1", "test.tool", succeeded: true, error: null);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("req-orphan", entry.RequestId);
            Assert.Equal(AIRequestTraceState.Running, entry.State);
            Assert.Single(entry.ToolCalls);
        }

        [Fact]
        public void StartRequest_Trims_When_Exceeds_MaxEntries()
        {
            var log = new AIRequestTraceLog();
            const int maxEntries = RimMindDefaults.DebugMaxEntries;

            for (int i = 0; i < maxEntries + 50; i++)
                log.StartRequest($"req-{i}", "src", "model", "prompt");

            Assert.Equal(maxEntries, log.Entries.Count);
            Assert.Equal("req-50", log.Entries[0].RequestId);
        }
    }
}
