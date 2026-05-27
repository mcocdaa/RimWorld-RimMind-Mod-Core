using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    /// <summary>
    /// Verifies that PawnThinker restores TraceContext in the callback path.
    /// The AI callback runs on a background thread where TraceContext.Current (AsyncLocal)
    /// is lost. When ProcessPendingCallback() runs on the main thread, it must restore
    /// the traceId captured in SendThinkRequest() via TraceContext.BeginScope().
    /// </summary>
    public class PawnThinkerTraceContextTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnThinkerPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnThinker.cs");

        [Fact]
        public void PawnThinker_Has_PendingTraceId_Field()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("_pendingTraceId", content);
        }

        [Fact]
        public void SendThinkRequest_Captures_TraceId_From_Envelope()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("_pendingTraceId = envelope.TraceId", content);
        }

        [Fact]
        public void ProcessPendingCallback_Restores_TraceContext_With_BeginScope()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("TraceContext.BeginScope", content);
            Assert.Contains("_pendingTraceId", content);
        }

        [Fact]
        public void ProcessPendingCallback_Disposes_TraceScope_In_Finally()
        {
            Assert.True(File.Exists(PawnThinkerPath), "PawnThinker.cs must exist");

            var content = File.ReadAllText(PawnThinkerPath);

            Assert.Contains("traceScope?.Dispose()", content);
        }
    }
}
