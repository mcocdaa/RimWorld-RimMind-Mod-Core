using System;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Presentation.Tests.Queue
{
    public sealed class AIRequestQueueExecutorTests
    {
        [Fact]
        public async Task PausedQueue_DoesNotRunExecutorUntilResumed()
        {
            var queue = new AIRequestQueueImpl();
            var started = false;
            queue.PauseQueue();

            queue.Enqueue(CreateEnvelope("paused"), _ => { }, _ =>
            {
                started = true;
                return Task.FromResult(Success());
            });

            await Task.Yield();
            Assert.False(started);

            queue.ResumeQueue();
            queue.CurrentTick = 60;
            queue.Tick();

            await Task.Delay(20);
            Assert.True(started);
        }

        [Fact]
        public async Task CancelActiveExecutor_InvokesCallbackExactlyOnce()
        {
            var queue = new AIRequestQueueImpl();
            var callbacks = 0;
            var completion = new TaskCompletionSource<Result<LlmResponse, RimMindError>>();

            queue.Enqueue(CreateEnvelope("cancel-once"), _ => Interlocked.Increment(ref callbacks), async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return Success();
            });

            Assert.True(queue.CancelRequest("cancel-once"));
            await Task.Delay(20);
            queue.Tick();
            queue.Tick();

            Assert.Equal(1, callbacks);
        }

        [Fact]
        public void ExpiredQueuedRequest_CompletesCallbackWithTimeout()
        {
            var queue = new AIRequestQueueImpl { CurrentTick = 0 };
            Result<LlmResponse, RimMindError>? callbackResult = null;
            queue.PauseQueue();

            queue.Enqueue(new LlmRequestEnvelope
            {
                RequestId = "expired-in-queue",
                ModId = "QueueTests",
                ExpireAtTicks = 1
            }, result => callbackResult = result, _ => Task.FromResult(Success()));

            queue.ResumeQueue();
            queue.CurrentTick = 60;
            queue.Tick();
            queue.Tick();

            Assert.NotNull(callbackResult);
            var result = callbackResult!.Value;
            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.Timeout, result.Error.Code);
            Assert.Equal(0, queue.TotalQueuedCount);
            Assert.Equal(0, queue.ActiveRequestCount);
        }

        private static LlmRequestEnvelope CreateEnvelope(string requestId) => new LlmRequestEnvelope
        {
            RequestId = requestId,
            ModId = "QueueTests"
        };

        private static Result<LlmResponse, RimMindError> Success() =>
            Result<LlmResponse, RimMindError>.Ok(new LlmResponse { Content = "ok" });
    }
}
