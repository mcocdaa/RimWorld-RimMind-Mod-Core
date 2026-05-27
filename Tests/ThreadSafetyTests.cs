using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class ContextRequestTests
    {
        [Fact]
        public void MaxTokens_Default_Is800()
        {
            var envelope = new LlmRequestEnvelope { ScenarioId = "test" };
            Assert.Equal(800, envelope.MaxTokens);
        }

        [Fact]
        public void MaxTokens_CanBeOverridden()
        {
            var envelope = new LlmRequestEnvelope { ScenarioId = "test", MaxTokens = 1600 };
            Assert.Equal(1600, envelope.MaxTokens);
        }
    }
}
