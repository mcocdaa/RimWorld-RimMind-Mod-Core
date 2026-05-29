using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-O6: ToolCall Audit Log.
    /// Verifies that ToolCallDispatchMiddleware logs successful tool executions
    /// with structured format including toolName, toolCallId, and npcId.
    /// </summary>
    public class P1_O6_ToolCallAuditLogTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string MiddlewarePath = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Pipeline", "Unified",
            "ToolCallDispatchMiddleware.cs");

        [Fact]
        public void ToolCallDispatchMiddleware_Logs_Successful_Dispatch()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Contains("[RimMind.ToolCall] action=Dispatched", source);
        }

        [Fact]
        public void ToolCallDispatchMiddleware_Success_Log_Includes_ToolName()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Contains("toolName=", source);
        }

        [Fact]
        public void ToolCallDispatchMiddleware_Success_Log_Includes_ToolCallId()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Contains("toolCallId=", source);
        }

        [Fact]
        public void ToolCallDispatchMiddleware_Success_Log_Includes_NpcId()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Contains("npcId=", source);
        }

        [Fact]
        public void ToolCallDispatchMiddleware_Logs_Failure_With_Structured_Format()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Contains("[RimMind.ToolCall] action=Failed", source);
        }

        [Fact]
        public void ToolCallDispatchMiddleware_Failure_Log_Includes_Error()
        {
            var source = File.ReadAllText(MiddlewarePath);
            Assert.Matches(@"error=", source);
        }
    }
}
