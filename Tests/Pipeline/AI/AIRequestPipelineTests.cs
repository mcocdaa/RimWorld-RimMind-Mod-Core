using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Client;
using RimMind.Core.Pipeline.AI;
using RimMind.Core.Runtime;
using RimMind.Kernel.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline.AI
{
    internal sealed class AITestErrorMiddleware : IMiddleware<AIRequestContext>
    {
        private readonly string _errorMessage;
        public int InvokeCount { get; private set; }

        public AITestErrorMiddleware(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public string Id => "AITestErrorMiddleware";
        public string Name => "AITestErrorMiddleware";
        public int Order => 100;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            InvokeCount++;
            context.Error = new Exception(_errorMessage);
            context.Response = AIResponse.Failure(context.Request.RequestId, _errorMessage);
            return Task.CompletedTask;
        }
    }

    internal sealed class AITestSuccessMiddleware : IMiddleware<AIRequestContext>
    {
        public int InvokeCount { get; private set; }

        public string Id => "AITestSuccessMiddleware";
        public string Name => "AITestSuccessMiddleware";
        public int Order => 100;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            InvokeCount++;
            context.Response = AIResponse.Ok(context.Request.RequestId, "ok", 10);
            return Task.CompletedTask;
        }
    }

    internal sealed class AITestThrowMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => "AITestThrowMiddleware";
        public string Name => "AITestThrowMiddleware";
        public int Order => 100;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            throw new InvalidOperationException("boom");
        }
    }

    public class AIRequestPipelineTests
    {
        private static AIRequestContext CreateContext(
            string systemPrompt = "sys",
            string userPrompt = "user",
            IAIClient? client = null)
        {
            return new AIRequestContext
            {
                Request = new AIRequest
                {
                    RequestId = Guid.NewGuid().ToString("N").Substring(0, 8),
                    ModId = "test",
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                },
                Client = client,
            };
        }

        [Fact]
        public async Task ShortCircuit_SetsFailureResponse_WhenShutdown()
        {
            RimMindRuntime.Initialize();
            try
            {
                RimMindRuntime.Instance.IsShutdown = true;

                var middleware = new ShortCircuitMiddleware();
                var context = CreateContext(client: null);
                var pipeline = new Pipeline<AIRequestContext>(new[] { middleware });

                await pipeline.ExecuteAsync(context);

                Assert.True(context.IsShortCircuited);
                Assert.NotNull(context.Response);
                Assert.False(context.Response.Success);
                Assert.Equal("shutdown", context.ShortCircuitReason);
            }
            finally
            {
                RimMindRuntime.Instance.IsShutdown = false;
                RimMindRuntime.Instance.Dispose();
            }
        }

        [Fact]
        public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
        {
            var cb = new CircuitBreakerMiddleware();
            var fail = new AITestErrorMiddleware("timeout");

            var pipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { cb, fail });

            for (int i = 0; i < 5; i++)
            {
                var ctx = CreateContext();
                await pipeline.ExecuteAsync(ctx);
            }

            var context = CreateContext();
            await pipeline.ExecuteAsync(context);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("circuit_open", context.ShortCircuitReason);
        }

        [Fact]
        public async Task CircuitBreaker_TransitionsToHalfOpen_AfterCooldown()
        {
            var cb = new CircuitBreakerMiddleware();
            var fail = new AITestErrorMiddleware("timeout");

            var failPipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { cb, fail });

            for (int i = 0; i < 5; i++)
            {
                var ctx = CreateContext();
                await failPipeline.ExecuteAsync(ctx);
            }

            var openedAtField = typeof(CircuitBreakerMiddleware).GetField("_openedAtUtc",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField!.SetValue(cb, DateTime.UtcNow.AddSeconds(-61));

            var success = new AITestSuccessMiddleware();
            var successPipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { cb, success });

            var context = CreateContext();
            await successPipeline.ExecuteAsync(context);

            Assert.False(context.IsShortCircuited);
            Assert.NotNull(context.Response);
            Assert.True(context.Response.Success);
        }

        [Fact]
        public async Task Retry_RetriesOnTransientError()
        {
            var retry = new RetryMiddleware();
            int invokeCount = 0;

            var context = CreateContext();

            await retry.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                ctx.Error = new Exception("timeout");
                ctx.Response = AIResponse.Failure(ctx.Request.RequestId, "timeout");
                return Task.CompletedTask;
            });

            Assert.Equal(3, invokeCount);
            Assert.Equal(2, context.RetryCount);
        }

        [Fact]
        public async Task Retry_DoesNotRetryOnNonTransientError()
        {
            var retry = new RetryMiddleware();
            int invokeCount = 0;

            var context = CreateContext();

            await retry.InvokeAsync(context, ctx =>
            {
                invokeCount++;
                ctx.Error = new Exception("invalid_api_key");
                ctx.Response = AIResponse.Failure(ctx.Request.RequestId, "invalid_api_key");
                return Task.CompletedTask;
            });

            Assert.Equal(1, invokeCount);
            Assert.Equal(0, context.RetryCount);
        }

        [Fact]
        public async Task Cache_HitsOnSameRequest()
        {
            var cache = new CacheMiddleware();
            var success = new AITestSuccessMiddleware();

            var pipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { cache, success });

            var ctx1 = CreateContext(systemPrompt: "sys", userPrompt: "user");
            await pipeline.ExecuteAsync(ctx1);

            var ctx2 = CreateContext(systemPrompt: "sys", userPrompt: "user");
            await pipeline.ExecuteAsync(ctx2);

            Assert.Equal(1, success.InvokeCount);
            Assert.True(ctx2.IsShortCircuited);
            Assert.Equal("cache_hit", ctx2.ShortCircuitReason);
        }

        [Fact]
        public async Task Cache_MissesOnDifferentRequest()
        {
            var cache = new CacheMiddleware();
            var success = new AITestSuccessMiddleware();

            var pipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { cache, success });

            var ctx1 = CreateContext(systemPrompt: "sys", userPrompt: "user1");
            await pipeline.ExecuteAsync(ctx1);

            var ctx2 = CreateContext(systemPrompt: "sys", userPrompt: "user2");
            await pipeline.ExecuteAsync(ctx2);

            Assert.Equal(2, success.InvokeCount);
            Assert.False(ctx2.IsShortCircuited);
        }

        [Fact]
        public async Task Telemetry_RecordsEvenOnException()
        {
            RimMindRuntime.Initialize();
            try
            {
                var telemetry = new TelemetryMiddleware();
                var throwing = new AITestThrowMiddleware();

                var pipeline = new Pipeline<AIRequestContext>(new IMiddleware<AIRequestContext>[] { telemetry, throwing });

                var context = CreateContext();
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => pipeline.ExecuteAsync(context));

                Assert.True(context.Elapsed > TimeSpan.Zero);
            }
            finally
            {
                RimMindRuntime.Instance.Dispose();
            }
        }
    }
}
