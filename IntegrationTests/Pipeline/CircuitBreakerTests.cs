using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.IntegrationTests.Pipeline
{
    [Collection("RimWorld Integration")]
    public class CircuitBreakerTests : TestBase
    {
        public CircuitBreakerTests(TestWorldFixture fixture) : base(fixture) { }

        /// <summary>
        /// After consecutive failures reach the threshold,
        /// the circuit should transition from Closed to Open
        /// and short-circuit subsequent requests with CircuitOpen error.
        /// </summary>
        [Fact]
        public async Task ConsecutiveFailures_ShouldOpenCircuit()
        {
            // Arrange - Use a low threshold for testing
            var settings = new TestCircuitBreakerSettings(failureThreshold: 2, openDurationSec: 60);
            var middleware = new CircuitBreakerMiddleware(settings);

            // Act - Execute enough failing requests to open the circuit
            for (int i = 0; i < 2; i++)
            {
                var context = CreateContext($"fail-{i}");
                await middleware.InvokeAsync(context, FailingNext);
                context.Result!.Value.IsErr.Should().BeTrue();
            }

            // The next request should be short-circuited by the open circuit
            var openContext = CreateContext("after-open");
            await middleware.InvokeAsync(openContext, _ => Task.CompletedTask);

            // Assert
            openContext.IsShortCircuited.Should().BeTrue();
            openContext.ShortCircuitReason.Should().Be("circuit_open");
            openContext.Result.Should().NotBeNull();
            openContext.Result!.Value.IsErr.Should().BeTrue();
            openContext.Result.Value.Error.Code.Should().Be(RimMindErrorCode.ClientCircuitOpen);
        }

        /// <summary>
        /// After the open duration expires, the circuit should transition
        /// from Open to HalfOpen, allowing one request through.
        /// </summary>
        [Fact]
        public async Task OpenDurationExpired_ShouldEnterHalfOpen()
        {
            // Arrange - Use a very short open duration
            var settings = new TestCircuitBreakerSettings(failureThreshold: 2, openDurationSec: 0);
            var middleware = new CircuitBreakerMiddleware(settings);

            // Open the circuit first
            for (int i = 0; i < 2; i++)
            {
                var context = CreateContext($"fail-{i}");
                await middleware.InvokeAsync(context, FailingNext);
            }

            // Wait briefly to ensure open duration has expired (0 seconds = immediate)
            await Task.Delay(50);

            // Act - This request should be allowed through (HalfOpen state)
            var halfOpenContext = CreateContext("half-open");
            await middleware.InvokeAsync(halfOpenContext, FailingNext);

            // Assert - Request was not short-circuited (it went through)
            halfOpenContext.IsShortCircuited.Should().BeFalse();
            // The result is an error from the failing next, not from circuit breaker
            halfOpenContext.Result!.Value.IsErr.Should().BeTrue();
            halfOpenContext.Result.Value.Error.Code.Should().NotBe(RimMindErrorCode.ClientCircuitOpen);
        }

        /// <summary>
        /// In HalfOpen state, a successful request should transition
        /// the circuit back to Closed state.
        /// </summary>
        [Fact]
        public async Task HalfOpenSuccess_ShouldCloseCircuit()
        {
            // Arrange
            var settings = new TestCircuitBreakerSettings(failureThreshold: 2, openDurationSec: 0);
            var middleware = new CircuitBreakerMiddleware(settings);

            // Open the circuit
            for (int i = 0; i < 2; i++)
            {
                var context = CreateContext($"fail-{i}");
                await middleware.InvokeAsync(context, FailingNext);
            }

            // Wait for open duration to expire
            await Task.Delay(50);

            // Act - Send a successful request in HalfOpen state
            var halfOpenContext = CreateContext("half-open-success");
            await middleware.InvokeAsync(halfOpenContext, SuccessfulNext);

            // Assert - Circuit should now be closed, allowing normal requests
            halfOpenContext.IsShortCircuited.Should().BeFalse();
            halfOpenContext.Result!.Value.IsOk.Should().BeTrue();

            // Verify circuit is closed by sending another successful request
            var closedContext = CreateContext("after-close");
            await middleware.InvokeAsync(closedContext, SuccessfulNext);
            closedContext.IsShortCircuited.Should().BeFalse();
            closedContext.Result!.Value.IsOk.Should().BeTrue();
        }

        private static LlmRequestContext CreateContext(string requestId)
        {
            var envelope = new LlmRequestEnvelope
            {
                RequestId = requestId,
                ScenarioId = "test",
                ModId = "RimMindCore",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "test" }
                }
            };
            return new LlmRequestContext(envelope);
        }

        private static Task FailingNext(LlmRequestContext context)
        {
            context.Result = Result<LlmResponse, RimMindError>.Err(
                RimMindErrors.ClientTransient("Simulated failure"));
            return Task.CompletedTask;
        }

        private static Task SuccessfulNext(LlmRequestContext context)
        {
            context.Result = Result<LlmResponse, RimMindError>.Ok(new LlmResponse
            {
                Content = "success",
                TokensUsed = 5
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Test implementation of ICircuitBreakerSettings with configurable values.
        /// </summary>
        private sealed class TestCircuitBreakerSettings : ICircuitBreakerSettings
        {
            public int CircuitBreakerFailureThreshold { get; }
            public int CircuitBreakerOpenDurationSec { get; }

            public TestCircuitBreakerSettings(int failureThreshold, int openDurationSec)
            {
                CircuitBreakerFailureThreshold = failureThreshold;
                CircuitBreakerOpenDurationSec = openDurationSec;
            }
        }
    }
}
