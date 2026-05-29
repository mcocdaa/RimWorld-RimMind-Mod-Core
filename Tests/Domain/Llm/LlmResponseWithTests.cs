using Xunit;

namespace RimMind.Tests.Domain.Llm
{
    public class LlmResponseWithTests
    {
        private static LlmResponse CreateSample()
        {
            return new LlmResponse
            {
                RequestId = "req-001",
                Content = "hello",
                ToolCallsJson = "[{\"name\":\"act\"}]",
                ReasoningContent = "thinking",
                TokensUsed = 100,
                PromptTokens = 60,
                CompletionTokens = 40,
                CachedTokens = 10,
                State = AIRequestState.Completed,
                Priority = AIRequestPriority.Normal,
                AttemptCount = 1,
                QueueWaitMs = 50,
                ProcessingMs = 200,
                HttpStatusCode = 200,
            };
        }

        [Fact]
        public void With_NoArgs_ReturnsCopyWithSameValues()
        {
            var original = CreateSample();
            var copy = original.With();

            Assert.NotSame(original, copy);
            Assert.Equal(original.RequestId, copy.RequestId);
            Assert.Equal(original.Content, copy.Content);
            Assert.Equal(original.ToolCallsJson, copy.ToolCallsJson);
            Assert.Equal(original.ReasoningContent, copy.ReasoningContent);
            Assert.Equal(original.TokensUsed, copy.TokensUsed);
            Assert.Equal(original.PromptTokens, copy.PromptTokens);
            Assert.Equal(original.CompletionTokens, copy.CompletionTokens);
            Assert.Equal(original.CachedTokens, copy.CachedTokens);
            Assert.Equal(original.State, copy.State);
            Assert.Equal(original.Priority, copy.Priority);
            Assert.Equal(original.AttemptCount, copy.AttemptCount);
            Assert.Equal(original.QueueWaitMs, copy.QueueWaitMs);
            Assert.Equal(original.ProcessingMs, copy.ProcessingMs);
            Assert.Equal(original.HttpStatusCode, copy.HttpStatusCode);
        }

        [Fact]
        public void With_OriginalUnmodified_AfterWithCall()
        {
            var original = CreateSample();
            original.With(content: "changed", tokensUsed: 999);

            Assert.Equal("hello", original.Content);
            Assert.Equal(100, original.TokensUsed);
        }

        [Fact]
        public void With_SingleStringField_OverridesOnlyThatField()
        {
            var original = CreateSample();
            var modified = original.With(content: "world");

            Assert.Equal("world", modified.Content);
            Assert.Equal(original.RequestId, modified.RequestId);
            Assert.Equal(original.TokensUsed, modified.TokensUsed);
            Assert.Equal(original.State, modified.State);
        }

        [Fact]
        public void With_SingleIntField_OverridesOnlyThatField()
        {
            var original = CreateSample();
            var modified = original.With(tokensUsed: 500);

            Assert.Equal(500, modified.TokensUsed);
            Assert.Equal(original.Content, modified.Content);
            Assert.Equal(original.PromptTokens, modified.PromptTokens);
        }

        [Fact]
        public void With_SingleLongField_OverridesOnlyThatField()
        {
            var original = CreateSample();
            var modified = original.With(processingMs: 9999);

            Assert.Equal(9999, modified.ProcessingMs);
            Assert.Equal(original.QueueWaitMs, modified.QueueWaitMs);
            Assert.Equal(original.HttpStatusCode, modified.HttpStatusCode);
        }

        [Fact]
        public void With_EnumField_OverridesOnlyThatField()
        {
            var original = CreateSample();
            var modified = original.With(state: AIRequestState.Error);

            Assert.Equal(AIRequestState.Error, modified.State);
            Assert.Equal(original.Priority, modified.Priority);
        }

        [Fact]
        public void With_MultipleFields_OverridesAllSpecified()
        {
            var original = CreateSample();
            var modified = original.With(
                priority: AIRequestPriority.Critical,
                attemptCount: 3,
                processingMs: 1500);

            Assert.Equal(AIRequestPriority.Critical, modified.Priority);
            Assert.Equal(3, modified.AttemptCount);
            Assert.Equal(1500, modified.ProcessingMs);
            Assert.Equal(original.RequestId, modified.RequestId);
            Assert.Equal(original.Content, modified.Content);
            Assert.Equal(original.State, modified.State);
        }

        [Fact]
        public void With_NullableStringField_SetToNonNull()
        {
            var original = CreateSample();
            var modified = original.With(toolCallsJson: "[{\"name\":\"new\"}]");

            Assert.Equal("[{\"name\":\"new\"}]", modified.ToolCallsJson);
        }

        [Fact]
        public void With_DefaultResponse_AllFieldsPreserved()
        {
            var original = new LlmResponse();
            var copy = original.With();

            Assert.NotSame(original, copy);
            Assert.Equal(string.Empty, copy.RequestId);
            Assert.Equal(string.Empty, copy.Content);
            Assert.Null(copy.ToolCallsJson);
            Assert.Null(copy.ReasoningContent);
            Assert.Equal(0, copy.TokensUsed);
            Assert.Equal(0, copy.PromptTokens);
            Assert.Equal(0, copy.CompletionTokens);
            Assert.Equal(0, copy.CachedTokens);
            Assert.Equal(AIRequestState.Queued, copy.State);
            Assert.Equal(AIRequestPriority.Low, copy.Priority);
            Assert.Equal(0, copy.AttemptCount);
            Assert.Equal(0, copy.QueueWaitMs);
            Assert.Equal(0, copy.ProcessingMs);
            Assert.Equal(0, copy.HttpStatusCode);
        }

        [Fact]
        public void With_ChainedCalls_AccumulateChanges()
        {
            var original = CreateSample();
            var modified = original
                .With(content: "step1")
                .With(tokensUsed: 200)
                .With(state: AIRequestState.Processing);

            Assert.Equal("step1", modified.Content);
            Assert.Equal(200, modified.TokensUsed);
            Assert.Equal(AIRequestState.Processing, modified.State);
            Assert.Equal(original.RequestId, modified.RequestId);
        }
    }
}
