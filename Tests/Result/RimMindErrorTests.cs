using System;
using RimMind.Domain.Events.Result;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    public class RimMindErrorTests
    {
        [Fact]
        public void Factory_Sets_TraceId_From_TraceContext()
        {
            using (TraceContext.BeginScope("trace-abc"))
            {
                var error = RimMindErrors.ClientNotConfigured("TestSource");
                Assert.Equal("trace-abc", error.TraceId);
            }
        }

        [Fact]
        public void Factory_Without_TraceContext_TraceId_Is_Null()
        {
            var error = RimMindErrors.Internal("something broke");
            Assert.Null(error.TraceId);
        }

        [Fact]
        public void ClientTransient_Sets_Code_And_TraceId()
        {
            using (TraceContext.BeginScope("t-123"))
            {
                var error = RimMindErrors.ClientTransient("transient fail");
                Assert.Equal(RimMindErrorCode.ClientTransientFailure, error.Code);
                Assert.Equal("t-123", error.TraceId);
            }
        }

        [Fact]
        public void ClientPermanent_With_InnerException()
        {
            var inner = new InvalidOperationException("inner");
            var error = RimMindErrors.ClientPermanent("permanent fail", inner);
            Assert.Equal(RimMindErrorCode.ClientPermanentFailure, error.Code);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void ToString_With_TraceId_Includes_Trace()
        {
            var error = new RimMindError(RimMindErrorCode.Timeout, "timed out") { TraceId = "xyz" };
            var s = error.ToString();
            Assert.Equal("[Timeout] timed out (trace=xyz)", s);
        }

        [Fact]
        public void ToString_Without_TraceId_Omits_Trace()
        {
            var error = new RimMindError(RimMindErrorCode.Cancelled, "cancelled");
            var s = error.ToString();
            Assert.Equal("[Cancelled] cancelled", s);
        }

        [Fact]
        public void Has_All_Required_Fields()
        {
            var inner = new Exception("boom");
            var details = new System.Collections.Generic.Dictionary<string, object?>
            {
                ["key"] = "value"
            };
            var error = new RimMindError(RimMindErrorCode.ToolExecutionFailed, "tool fail")
            {
                TraceId = "t-1",
                Source = "ToolRunner",
                Details = details,
                InnerException = inner
            };
            Assert.Equal(RimMindErrorCode.ToolExecutionFailed, error.Code);
            Assert.Equal("tool fail", error.Message);
            Assert.Equal("t-1", error.TraceId);
            Assert.Equal("ToolRunner", error.Source);
            Assert.NotNull(error.Details);
            Assert.Equal("value", error.Details!["key"]);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void ToolNotFound_Contains_Details_With_ToolId()
        {
            var error = RimMindErrors.ToolNotFound("my-tool");
            Assert.Equal(RimMindErrorCode.ToolNotFound, error.Code);
            Assert.NotNull(error.Details);
            Assert.Equal("my-tool", error.Details!["tool_id"]);
        }

        [Fact]
        public void ToolMaxDepthExceeded_Contains_Details_With_Depth()
        {
            var error = RimMindErrors.ToolMaxDepthExceeded(5);
            Assert.Equal(RimMindErrorCode.ToolMaxDepthExceeded, error.Code);
            Assert.NotNull(error.Details);
            Assert.Equal(5, error.Details!["max_depth"]);
        }

        [Fact]
        public void NpcNotFound_Contains_Details_With_NpcId()
        {
            var error = RimMindErrors.NpcNotFound("npc-42");
            Assert.Equal(RimMindErrorCode.NpcNotFound, error.Code);
            Assert.NotNull(error.Details);
            Assert.Equal("npc-42", error.Details!["npc_id"]);
        }

        [Fact]
        public void CircuitOpen_Has_Correct_Code()
        {
            var error = RimMindErrors.CircuitOpen();
            Assert.Equal(RimMindErrorCode.ClientCircuitOpen, error.Code);
        }

        [Fact]
        public void NotImplemented_Factory_Sets_Code()
        {
            var error = RimMindErrors.NotImplemented("not yet");
            Assert.Equal(RimMindErrorCode.NotImplemented, error.Code);
        }
    }
}
