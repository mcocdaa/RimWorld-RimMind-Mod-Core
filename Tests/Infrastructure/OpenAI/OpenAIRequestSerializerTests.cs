using System;
using RimMind.Domain.Llm;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Application.Features.Agent.Modes;
using Xunit;

namespace RimMind.Tests.Infrastructure.OpenAI
{
    public class OpenAIRequestSerializerTests
    {
        // Mirrors the schema string set by ProactiveThinkStrategy.BuildEnvelope and
        // ReactiveAgentMode: a non-JSON sentinel for the <Action>{...}</Action> text convention.
        private const string DecisionSchemaSentinel = "<Action>...</Action>";

        private static LlmRequestEnvelope DecisionShapedEnvelope()
        {
            return new LlmRequestEnvelope
            {
                RequestId = "test-req",
                ScenarioId = ScenarioIds.Decision,
                Messages = { new ChatMessage { Role = "user", Content = "<perceptions>hungry</perceptions>" } },
                Examples = ThinkStrategyHelper.BuildDecisionExamples(),
                JsonSchema = DecisionSchemaSentinel,
                Temperature = 0.7f,
                MaxTokens = 800,
            };
        }

        [Fact]
        public void DecisionEnvelope_WithSentinelSchema_DoesNotThrow()
        {
            var envelope = DecisionShapedEnvelope();

            // CURRENTLY FAILS: JsonConvert.DeserializeObject("<Action>...</Action>") throws JsonReaderException.
            var ex = Record.Exception(() =>
                OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800));

            Assert.Null(ex);
        }

        [Fact]
        public void ValidJsonSchema_StillEmitsResponseFormat()
        {
            var envelope = new LlmRequestEnvelope
            {
                RequestId = "test-req",
                ScenarioId = ScenarioIds.Memory,
                Messages = { new ChatMessage { Role = "user", Content = "summarize" } },
                JsonSchema = "{\"type\":\"object\",\"properties\":{\"summary\":{\"type\":\"string\"}}}",
            };

            string json = OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800);

            Assert.Contains("\"response_format\"", json);
            Assert.Contains("\"json_schema\"", json);
        }
    }
}
