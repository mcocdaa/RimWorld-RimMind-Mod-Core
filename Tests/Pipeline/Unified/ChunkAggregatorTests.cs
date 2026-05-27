using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class ChunkAggregatorTests
    {
        [Fact]
        public void Append_TextChunks_BuildsContent()
        {
            var aggregator = new ChunkAggregator("req-1");

            aggregator.Append(new LlmChunk { DeltaContent = "Hello" });
            aggregator.Append(new LlmChunk { DeltaContent = " " });
            aggregator.Append(new LlmChunk { DeltaContent = "World" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("Hello World", result.Value.Content);
            Assert.Equal("req-1", result.Value.RequestId);
            Assert.Equal(AIRequestState.Completed, result.Value.State);
        }

        [Fact]
        public void Append_ToolCallsChunks_BuildsToolCallsJson()
        {
            var aggregator = new ChunkAggregator("req-2");

            aggregator.Append(new LlmChunk { DeltaToolCallsJson = "[{\"id\":" });
            aggregator.Append(new LlmChunk { DeltaToolCallsJson = "\"tc-1\"}]" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("[{\"id\":\"tc-1\"}]", result.Value.ToolCallsJson);
        }

        [Fact]
        public void Append_MixedChunks_AggregatesBoth()
        {
            var aggregator = new ChunkAggregator("req-3");

            aggregator.Append(new LlmChunk { DeltaContent = "Thinking" });
            aggregator.Append(new LlmChunk { DeltaToolCallsJson = "[{\"id\":" });
            aggregator.Append(new LlmChunk { DeltaContent = "..." });
            aggregator.Append(new LlmChunk { DeltaToolCallsJson = "\"tc-1\"}]" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("Thinking...", result.Value.Content);
            Assert.Equal("[{\"id\":\"tc-1\"}]", result.Value.ToolCallsJson);
        }

        [Fact]
        public void BuildFinalResponse_NoToolCalls_ToolCallsJsonIsNull()
        {
            var aggregator = new ChunkAggregator("req-4");

            aggregator.Append(new LlmChunk { DeltaContent = "just text" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("just text", result.Value.Content);
            Assert.Null(result.Value.ToolCallsJson);
        }

        [Fact]
        public void CreateFinalChunk_HasIsLastTrueAndFinalResponse()
        {
            var aggregator = new ChunkAggregator("req-5");

            aggregator.Append(new LlmChunk { DeltaContent = "done" });

            var chunk = aggregator.CreateFinalChunk();
            Assert.True(chunk.IsLast);
            Assert.NotNull(chunk.FinalResponse);
            Assert.Equal("done", chunk.FinalResponse.Content);
            Assert.Null(chunk.DeltaContent);
            Assert.Null(chunk.DeltaToolCallsJson);
        }

        [Fact]
        public void ChunkCount_TracksAppends()
        {
            var aggregator = new ChunkAggregator("req-6");

            Assert.Equal(0, aggregator.ChunkCount);

            aggregator.Append(new LlmChunk { DeltaContent = "a" });
            aggregator.Append(new LlmChunk { DeltaContent = "b" });
            aggregator.Append(new LlmChunk { DeltaContent = "c" });

            Assert.Equal(3, aggregator.ChunkCount);
        }

        [Fact]
        public void Append_NullDeltaContent_DoesNotCrash()
        {
            var aggregator = new ChunkAggregator("req-7");

            aggregator.Append(new LlmChunk()); // null DeltaContent and DeltaToolCallsJson

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("", result.Value.Content);
            Assert.Null(result.Value.ToolCallsJson);
            Assert.Equal(1, aggregator.ChunkCount);
        }

        [Fact]
        public void Append_WithReasoningContent_AccumulatesCorrectly()
        {
            var aggregator = new ChunkAggregator("req-8");

            aggregator.Append(new LlmChunk { DeltaReasoningContent = "Step 1: " });
            aggregator.Append(new LlmChunk { DeltaReasoningContent = "Analyze" });
            aggregator.Append(new LlmChunk { DeltaReasoningContent = " input" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("Step 1: Analyze input", result.Value.ReasoningContent);
        }

        [Fact]
        public void Append_WithUsageInfo_PopulatesTokenCounts()
        {
            var aggregator = new ChunkAggregator("req-9");

            aggregator.Append(new LlmChunk { DeltaContent = "Hello" });
            aggregator.Append(new LlmChunk
            {
                DeltaPromptTokens = 50,
                DeltaCompletionTokens = 20,
                DeltaCachedTokens = 10,
            });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal(50, result.Value.PromptTokens);
            Assert.Equal(20, result.Value.CompletionTokens);
            Assert.Equal(10, result.Value.CachedTokens);
        }

        [Fact]
        public void BuildFinalResponse_WithMetadata_AllFieldsPopulated()
        {
            var aggregator = new ChunkAggregator("req-10");

            aggregator.Append(new LlmChunk { DeltaContent = "Result" });
            aggregator.Append(new LlmChunk { DeltaReasoningContent = "Thinking" });
            aggregator.Append(new LlmChunk
            {
                DeltaPromptTokens = 100,
                DeltaCompletionTokens = 50,
                DeltaCachedTokens = 30,
            });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal("Result", result.Value.Content);
            Assert.Equal("Thinking", result.Value.ReasoningContent);
            Assert.Equal(100, result.Value.PromptTokens);
            Assert.Equal(50, result.Value.CompletionTokens);
            Assert.Equal(30, result.Value.CachedTokens);
            Assert.Equal(150, result.Value.TokensUsed);
            Assert.Equal(AIRequestState.Completed, result.Value.State);
        }

        [Fact]
        public void BuildFinalResponse_WithoutMetadata_DefaultsToZero()
        {
            var aggregator = new ChunkAggregator("req-11");

            aggregator.Append(new LlmChunk { DeltaContent = "text only" });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Null(result.Value.ReasoningContent);
            Assert.Equal(0, result.Value.PromptTokens);
            Assert.Equal(0, result.Value.CompletionTokens);
            Assert.Equal(0, result.Value.CachedTokens);
            Assert.Equal(0, result.Value.TokensUsed);
            Assert.Equal(0, result.Value.AttemptCount);
            Assert.Equal(0, result.Value.QueueWaitMs);
            Assert.Equal(0, result.Value.ProcessingMs);
        }

        [Fact]
        public void BuildFinalResponse_TokensUsed_EqualsPromptPlusCompletion()
        {
            var aggregator = new ChunkAggregator("req-tokens");

            aggregator.Append(new LlmChunk { DeltaContent = "Hello" });
            aggregator.Append(new LlmChunk
            {
                DeltaPromptTokens = 100,
                DeltaCompletionTokens = 50,
            });

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsOk);
            Assert.Equal(150, result.Value.TokensUsed); // 100 + 50
            Assert.Equal(100, result.Value.PromptTokens);
            Assert.Equal(50, result.Value.CompletionTokens);
        }

        [Fact]
        public void BuildFinalResponse_WithError_ReturnsErr()
        {
            var aggregator = new ChunkAggregator("req-err-1");

            aggregator.SetError(RimMindErrors.ClientTransient("test error"));

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.ClientTransientFailure, result.Error.Code);
            Assert.Equal("test error", result.Error.Message);
        }

        [Fact]
        public void SetError_ThenBuildFinalResponse_ReturnsErr()
        {
            var aggregator = new ChunkAggregator("req-err-2");

            aggregator.Append(new LlmChunk { DeltaContent = "Hello" });
            aggregator.Append(new LlmChunk { DeltaToolCallsJson = "[{\"id\":\"tc-1\"}]" });
            aggregator.Append(new LlmChunk
            {
                DeltaPromptTokens = 50,
                DeltaCompletionTokens = 20,
            });

            aggregator.SetError(RimMindErrors.ClientTransient("mid-stream failure"));

            var result = aggregator.BuildFinalResponse();
            Assert.True(result.IsErr);
            Assert.Equal(RimMindErrorCode.ClientTransientFailure, result.Error.Code);
            Assert.Equal("mid-stream failure", result.Error.Message);
        }

        [Fact]
        public void CreateFinalChunk_WithError_FinalResponseIsNull()
        {
            var aggregator = new ChunkAggregator("req-err-3");

            aggregator.Append(new LlmChunk { DeltaContent = "partial" });
            aggregator.SetError(RimMindErrors.ClientTransient("stream aborted"));

            var chunk = aggregator.CreateFinalChunk();
            Assert.True(chunk.IsLast);
            Assert.Null(chunk.FinalResponse);
            Assert.Null(chunk.DeltaContent);
            Assert.Null(chunk.DeltaToolCallsJson);
        }
    }
}
