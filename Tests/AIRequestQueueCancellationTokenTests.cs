using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Client;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Queue;
using Verse;
using Xunit;

using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Tests
{
    public class AIRequestQueueCancellationTokenTests
    {
        public AIRequestQueueCancellationTokenTests()
        {
            RimMindRuntime.Initialize();
        }

        private AIRequestQueueImpl CreateQueue()
        {
            RimMindCoreMod.Settings = new AICoreSettings
            {
                maxConcurrentRequests = 3,
                maxRetryCount = 2,
                requestTimeoutMs = 120000,
                queueProcessInterval = 60,
                defaultModCooldownTicks = 3600,
            };
            return new AIRequestQueueImpl();
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

            queue.Reset();

            Assert.True(tokenBefore.IsCancellationRequested);
            Assert.False(queue.GetCurrentCancellationToken().IsCancellationRequested);
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

            queue.Tick();

            Assert.NotNull(result);
            Assert.Equal(AIRequestState.Completed, result!.State);
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

            queue.Tick();

            Assert.NotNull(result);
            Assert.Equal(AIRequestState.Completed, result!.State);
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

            public Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request)
            {
                return Task.FromResult(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok(request.RequestId, "stub response", 10)));
            }

            public Task<Result<AIResponse, RimMindError>> SendStructuredAsync(AIRequest request, string? jsonSchema, List<StructuredTool>? tools)
            {
                return Task.FromResult(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok(request.RequestId, "stub structured", 10)));
            }
        }
    }

    public static class AIRequestQueueTestExtensions
    {
        public static CancellationToken GetCurrentCancellationToken(this AIRequestQueueImpl queue)
        {
            var field = typeof(AIRequestQueueImpl).GetField("_cts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(field);

            var cts = (CancellationTokenSource)field.GetValue(queue)!;
            return cts.Token;
        }
    }
}
