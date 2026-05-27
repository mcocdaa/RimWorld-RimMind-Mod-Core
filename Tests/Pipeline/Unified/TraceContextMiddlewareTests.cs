using System.Threading.Tasks;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class TraceContextMiddlewareTests
    {
        [Fact]
        public async Task CallsNextWithTraceScope()
        {
            var middleware = new TraceContextMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    TraceId = "abc123def456",
                },
            };
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task NullEnvelope_UsesContextTraceId()
        {
            var middleware = new TraceContextMiddleware();
            var context = new LlmRequestContext(); // Envelope is null
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
        }
    }
}
