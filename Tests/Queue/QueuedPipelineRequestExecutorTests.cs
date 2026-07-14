using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Presentation.Tests.Queue
{
    public sealed class QueuedPipelineRequestExecutorTests
    {
        [Fact]
        public void Constructor_WithEnvelope_ProvidesCallbackContextBeforeExecution()
        {
            var envelope = new LlmRequestEnvelope { RequestId = "cancelled-before-start" };
            var executor = new QueuedPipelineRequestExecutor(new RecordingPipeline(), new MockAIClient(), envelope);

            Assert.NotNull(executor.Context);
            Assert.Same(envelope, executor.Context!.Envelope);
        }

        [Fact]
        public async Task ExecuteAsync_PassesQueueCancellationTokenToRealPipelineContext()
        {
            var pipeline = new RecordingPipeline();
            var executor = new QueuedPipelineRequestExecutor(pipeline, new MockAIClient());
            using var cancellation = new CancellationTokenSource();

            var result = await executor.ExecuteAsync(new LlmRequestEnvelope { RequestId = "pipeline-queue" }, cancellation.Token);

            Assert.True(result.IsOk);
            Assert.NotNull(pipeline.Context);
            Assert.Equal(cancellation.Token, pipeline.Context!.Ct);
            Assert.Same(executor.Context, pipeline.Context);
        }

        private sealed class RecordingPipeline : IPipeline<LlmRequestContext>
        {
            public LlmRequestContext? Context { get; private set; }

            public Task ExecuteAsync(LlmRequestContext context)
            {
                Context = context;
                context.Result = Result<LlmResponse, RimMindError>.Ok(new LlmResponse { Content = "queued" });
                return Task.CompletedTask;
            }
        }
    }
}
