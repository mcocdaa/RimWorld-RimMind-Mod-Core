using System.Threading.Tasks;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class CacheMiddlewareTests
    {
        private static LlmRequestContext CreateContext(
            bool isStreaming = false,
            bool cacheHit = false)
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    IsStreaming = isStreaming,
                },
                CacheHit = cacheHit,
            };
        }

        [Fact]
        public async Task StreamingRequest_SkipsCacheAndCallsNext()
        {
            var middleware = new CacheMiddleware();
            var context = CreateContext(isStreaming: true, cacheHit: true);
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task CacheHit_ShortCircuits()
        {
            var middleware = new CacheMiddleware();
            var context = CreateContext(isStreaming: false, cacheHit: true);
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.False(nextCalled);
            Assert.True(context.IsShortCircuited);
            Assert.Equal("CacheHit", context.ShortCircuitReason);
        }

        [Fact]
        public async Task CacheMiss_CallsNext()
        {
            var middleware = new CacheMiddleware();
            var context = CreateContext(isStreaming: false, cacheHit: false);
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }
    }
}
