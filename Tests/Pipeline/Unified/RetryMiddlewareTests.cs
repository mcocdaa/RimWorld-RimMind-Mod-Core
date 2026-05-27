using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class RetryMiddlewareTests
    {
        private static LlmRequestContext CreateContext()
        {
            return new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                },
            };
        }

        [Fact]
        public async Task SuccessOnFirstAttempt_DoesNotRetry()
        {
            var middleware = new RetryMiddleware(maxRetries: 2, delay: TimeSpan.Zero);
            var context = CreateContext();
            int invokeCount = 0;

            await middleware.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "ok" });
                return Task.CompletedTask;
            });

            Assert.Equal(1, invokeCount);
            Assert.Equal(0, context.RetryCount);
        }

        [Fact]
        public async Task TransientError_RetriesUntilSuccess()
        {
            var middleware = new RetryMiddleware(maxRetries: 3, delay: TimeSpan.Zero);
            var context = CreateContext();
            int invokeCount = 0;

            await middleware.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                if (invokeCount < 3)
                {
                    ctx.Result = Result<LlmResponse, RimMindError>.Err(
                        RimMindErrors.ClientTransient("timeout"));
                }
                else
                {
                    ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                        new LlmResponse { RequestId = "req-1", Content = "ok" });
                }
                return Task.CompletedTask;
            });

            Assert.Equal(3, invokeCount);
            Assert.True(context.Result?.IsOk);
        }

        [Fact]
        public async Task PersistentError_ExhaustsRetries()
        {
            var middleware = new RetryMiddleware(maxRetries: 2, delay: TimeSpan.Zero);
            var context = CreateContext();
            int invokeCount = 0;

            await middleware.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                ctx.Result = Result<LlmResponse, RimMindError>.Err(
                    RimMindErrors.ClientTransient("timeout"));
                return Task.CompletedTask;
            });

            Assert.Equal(3, invokeCount); // 1 initial + 2 retries
            Assert.True(context.Result?.IsErr);
        }

        [Fact]
        public async Task ShortCircuitNonTransient_StopsRetrying()
        {
            var middleware = new RetryMiddleware(maxRetries: 3, delay: TimeSpan.Zero);
            var context = CreateContext();
            int invokeCount = 0;

            await middleware.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                ctx.Result = Result<LlmResponse, RimMindError>.Err(
                    RimMindErrors.ClientPermanent("invalid_key"));
                ctx.ShortCircuit("permanent_error");
                return Task.CompletedTask;
            });

            Assert.Equal(1, invokeCount);
        }
    }
}
