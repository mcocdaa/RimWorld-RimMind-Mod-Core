using System;
using System.Threading.Tasks;
using RimMind.Application.Features.Logging;
using Xunit;

namespace RimMind.Tests.Pipeline
{
    public class LoggerTraceIdTests
    {
        [Fact]
        public void BeginTraceScope_SetsAndRestoresTraceId()
        {
            string? beforeScope;
            using (RimMindLogger.BeginTraceScope(null!))
            {
                beforeScope = RimMindLogger.CurrentTraceId;
            }

            using (RimMindLogger.BeginTraceScope("abc123"))
            {
                Assert.Equal("abc123", RimMindLogger.CurrentTraceId);
            }

            Assert.Equal(beforeScope, RimMindLogger.CurrentTraceId);
        }

        [Fact]
        public void BeginTraceScope_NestedScopesRestorePreviousTraceId()
        {
            using (RimMindLogger.BeginTraceScope("outer"))
            {
                Assert.Equal("outer", RimMindLogger.CurrentTraceId);

                using (RimMindLogger.BeginTraceScope("inner"))
                {
                    Assert.Equal("inner", RimMindLogger.CurrentTraceId);
                }

                Assert.Equal("outer", RimMindLogger.CurrentTraceId);
            }
        }

        [Fact]
        public async Task BeginTraceScope_TraceIdAvailableInAsyncLocalWithinScope()
        {
            string? capturedInScope = null;
            string? capturedAfterScope = null;

            using (RimMindLogger.BeginTraceScope("async_trace"))
            {
                await Task.Run(() =>
                {
                    capturedInScope = RimMindLogger.CurrentTraceId;
                });
            }

            await Task.Run(() =>
            {
                capturedAfterScope = RimMindLogger.CurrentTraceId;
            });

            Assert.Equal("async_trace", capturedInScope);
            Assert.Null(capturedAfterScope);
        }
    }
}
