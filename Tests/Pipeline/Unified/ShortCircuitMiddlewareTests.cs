using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Runtime;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubRuntime : IRimMindRuntime
    {
        public bool IsShutdown { get; set; }
        public void AddMiddleware<TContext>(IMiddleware<TContext> middleware) where TContext : IPipelineContext { }
        public IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension => throw new NotImplementedException();
        public void Dispose() { }
    }

    public class ShortCircuitMiddlewareTests
    {
        private static LlmRequestContext CreateContext(
            string? requestId = "req-1")
        {
            return new LlmRequestContext
            {
                Envelope = requestId != null
                    ? new LlmRequestEnvelope { RequestId = requestId, ScenarioId = "test" }
                    : null!,
            };
        }

        [Fact]
        public async Task RuntimeShutdown_ShortCircuits()
        {
            var runtime = new StubRuntime { IsShutdown = true };
            var middleware = new ShortCircuitMiddleware(runtime: runtime);
            var context = CreateContext();
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(context.IsShortCircuited);
            Assert.Equal("runtime_shutdown", context.ShortCircuitReason);
            Assert.False(nextCalled);
            Assert.NotNull(context.Result);
            Assert.True(context.Result!.Value.IsErr);
        }

        [Fact]
        public async Task NullEnvelope_ShortCircuits()
        {
            var middleware = new ShortCircuitMiddleware();
            var context = new LlmRequestContext(); // Envelope is null!
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(context.IsShortCircuited);
            Assert.Equal("null_envelope", context.ShortCircuitReason);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task EmptyRequestId_ShortCircuits()
        {
            var middleware = new ShortCircuitMiddleware();
            var context = CreateContext(requestId: "");
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(context.IsShortCircuited);
            Assert.Equal("empty_request_id", context.ShortCircuitReason);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task ValidRequest_CallsNext()
        {
            var middleware = new ShortCircuitMiddleware();
            var context = CreateContext(requestId: "req-1");
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.False(context.IsShortCircuited);
            Assert.True(nextCalled);
        }
    }
}
