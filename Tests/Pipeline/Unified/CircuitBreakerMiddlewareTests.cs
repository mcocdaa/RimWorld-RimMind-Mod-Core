using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubCircuitBreakerSettings : ICircuitBreakerSettings
    {
        public int CircuitBreakerFailureThreshold { get; set; } = 3;
        public int CircuitBreakerOpenDurationSec { get; set; } = 60;
    }

    public class CircuitBreakerMiddlewareTests
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
        public async Task ClosedState_OnSuccess_StaysClosed()
        {
            var middleware = new CircuitBreakerMiddleware();
            var context = CreateContext();

            await middleware.InvokeAsync(context, ctx =>
            {
                ctx.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "ok" });
                return Task.CompletedTask;
            });

            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task OpensAfterConsecutiveFailures()
        {
            var settings = new StubCircuitBreakerSettings { CircuitBreakerFailureThreshold = 3 };
            var middleware = new CircuitBreakerMiddleware(settings: settings);

            // Trip the circuit with 3 failures
            for (int i = 0; i < 3; i++)
            {
                var ctx = CreateContext();
                await middleware.InvokeAsync(ctx, c =>
                {
                    c.Result = Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient("fail"));
                    return Task.CompletedTask;
                });
            }

            // Next request should be short-circuited
            var context = CreateContext();
            await middleware.InvokeAsync(context, c =>
            {
                c.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "ok" });
                return Task.CompletedTask;
            });

            Assert.True(context.IsShortCircuited);
            Assert.Equal("circuit_open", context.ShortCircuitReason);
        }

        [Fact]
        public async Task HalfOpen_AfterCooldown_AllowsRequest()
        {
            var settings = new StubCircuitBreakerSettings
            {
                CircuitBreakerFailureThreshold = 2,
                CircuitBreakerOpenDurationSec = 1,
            };
            var middleware = new CircuitBreakerMiddleware(settings: settings);

            // Trip the circuit
            for (int i = 0; i < 2; i++)
            {
                var ctx = CreateContext();
                await middleware.InvokeAsync(ctx, c =>
                {
                    c.Result = Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientTransient("fail"));
                    return Task.CompletedTask;
                });
            }

            // Wait for cooldown
            await Task.Delay(1100);

            // Should allow request (half-open -> success -> closed)
            var context = CreateContext();
            await middleware.InvokeAsync(context, c =>
            {
                c.Result = Result<LlmResponse, RimMindError>.Ok(
                    new LlmResponse { RequestId = "req-1", Content = "ok" });
                return Task.CompletedTask;
            });

            Assert.False(context.IsShortCircuited);
            Assert.True(context.Result?.IsOk);
        }
    }
}
