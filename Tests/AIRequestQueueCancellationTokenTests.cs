using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Core.Client;
using RimMind.Core.Internal;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    [CollectionDefinition("AIRequestQueueCancellationToken", DisableParallelization = true)]
    public class AIRequestQueueCancellationTokenCollection { }

    [Collection("AIRequestQueueCancellationToken")]
    public class AIRequestQueueCancellationTokenTests
    {
        private AIRequestQueue CreateQueue()
        {
            RimMindCoreMod.Settings = new AICoreSettings
            {
                maxConcurrentRequests = 3,
                maxRetryCount = 2,
                requestTimeoutMs = 120000,
                queueProcessInterval = 60,
                defaultModCooldownTicks = 3600,
            };
            return new AIRequestQueue(new Game());
        }

        [Fact]
        public void CancelAllRequests_CancelsExistingToken()
        {
            var queue = CreateQueue();
            var tokenBefore = queue.GetCurrentCancellationToken();

            Assert.False(tokenBefore.IsCancellationRequested);

            queue.CancelAllRequests();

            Assert.True(tokenBefore.IsCancellationRequested);
        }

        [Fact]
        public void CancelAllRequests_NewTokenIsNotCancelled()
        {
            var queue = CreateQueue();
            queue.CancelAllRequests();

            var tokenAfter = queue.GetCurrentCancellationToken();

            Assert.False(tokenAfter.IsCancellationRequested);
        }

        [Fact]
        public void CancelAllRequests_CalledTwice_BothOldTokensCancelled()
        {
            var queue = CreateQueue();
            var token1 = queue.GetCurrentCancellationToken();

            queue.CancelAllRequests();
            var token2 = queue.GetCurrentCancellationToken();

            queue.CancelAllRequests();
            var token3 = queue.GetCurrentCancellationToken();

            Assert.True(token1.IsCancellationRequested);
            Assert.True(token2.IsCancellationRequested);
            Assert.False(token3.IsCancellationRequested);
        }

        [Fact]
        public void LoadedGame_CancelsInFlightRequests()
        {
            var queue = CreateQueue();
            var tokenBefore = queue.GetCurrentCancellationToken();

            queue.LoadedGame();

            Assert.True(tokenBefore.IsCancellationRequested);
            Assert.False(queue.GetCurrentCancellationToken().IsCancellationRequested);
        }

        [Fact]
        public async Task FireRequest_WithCancelledToken_TaskRunRejectsCancelledToken()
        {
            var queue = CreateQueue();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            bool taskStarted = false;
            var task = Task.Run(async () =>
            {
                taskStarted = true;
                await Task.CompletedTask;
            }, cts.Token);

            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.False(taskStarted);
        }

        [Fact]
        public async Task FireRequest_WithCancelledToken_ThrowsBeforeLambdaStarts()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            bool lambdaExecuted = false;
            var task = Task.Run(async () =>
            {
                lambdaExecuted = true;
                await Task.CompletedTask;
            }, cts.Token);

            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.False(lambdaExecuted);
        }

        [Fact]
        public async Task FireRequest_WithoutCancellation_CompletesNormally()
        {
            var queue = CreateQueue();
            var client = new StubClient();
            var request = new AIRequest
            {
                RequestId = "test-normal",
                UserPrompt = "hello",
                ModId = "TestMod",
            };

            AIResponse? result = null;
            queue.EnqueueImmediate(request, r => result = r, client);

            await Task.Delay(200);

            queue.GameComponentTick();

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.Equal("stub response", result.Content);
        }

        [Fact]
        public async Task FireRequest_CancelThenNewRequest_WorksOnNewToken()
        {
            var queue = CreateQueue();
            queue.CancelAllRequests();

            var client = new StubClient();
            var request = new AIRequest
            {
                RequestId = "test-after-cancel",
                UserPrompt = "hello",
                ModId = "TestMod",
            };

            AIResponse? result = null;
            queue.EnqueueImmediate(request, r => result = r, client);

            await Task.Delay(200);

            queue.GameComponentTick();

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.Equal("stub response", result.Content);
        }

        [Fact]
        public void CancellationToken_InitializedAsCanBeCanceled()
        {
            var queue = CreateQueue();
            var token = queue.GetCurrentCancellationToken();

            Assert.True(token.CanBeCanceled);
            Assert.False(token.IsCancellationRequested);
        }

        private class StubClient : IAIClient
        {
            public bool IsLocalEndpoint => false;

            public bool IsConfigured() => true;

            public Task<AIResponse> SendAsync(AIRequest request)
            {
                return Task.FromResult(AIResponse.Ok(request.RequestId, "stub response", 10));
            }

            public Task<AIResponse> SendStructuredAsync(AIRequest request, string? jsonSchema, List<StructuredTool>? tools)
            {
                return Task.FromResult(AIResponse.Ok(request.RequestId, "stub structured", 10));
            }
        }
    }

    public static class AIRequestQueueTestExtensions
    {
        public static CancellationToken GetCurrentCancellationToken(this AIRequestQueue queue)
        {
            var field = typeof(AIRequestQueue).GetField("_cts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(field);

            var cts = (CancellationTokenSource)field.GetValue(queue)!;
            return cts.Token;
        }
    }
}
