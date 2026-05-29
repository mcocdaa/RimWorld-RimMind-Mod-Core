using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Newtonsoft.Json;
using RimMind.Domain.Llm;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Common.Models.Context;
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

        // Locate the snapshot file next to this test source (deterministic across machines/CI).
        private static string SnapshotPath([CallerFilePath] string thisFile = "")
        {
            string dir = Path.Combine(Path.GetDirectoryName(thisFile)!, "__snapshots__");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "decision-request.json");
        }

        // Set RIMMIND_UPDATE_SNAPSHOTS=1 to (re)generate the golden file after an intended change.
        private static bool ShouldRegenerate =>
            Environment.GetEnvironmentVariable("RIMMIND_UPDATE_SNAPSHOTS") == "1";

        private static LlmRequestEnvelope FullDecisionRequest()
        {
            // Representative system prompt. The real one is injected by ContextBuildMiddleware at
            // pipeline time and depends on live game state; this fixed string stands in for it so
            // the snapshot is deterministic and reviewable.
            const string systemPrompt =
                "You are an autonomous RimWorld colonist. Decide the single best next action. " +
                "Respond ONLY with <Action>{\"action\":\"mechanism.operation\",\"reason\":\"...\",\"param\":\"...\"}</Action>.";

            var tools = new List<StructuredTool>
            {
                new StructuredTool
                {
                    Name = "pawn.move.to",
                    Description = "Move the pawn to a target cell.",
                    Parameters = "{\"type\":\"object\",\"properties\":{\"target\":{\"type\":\"string\"}}}",
                    ToolChoice = "auto",
                },
            };

            return new LlmRequestEnvelope
            {
                RequestId = "snapshot-decision",
                ScenarioId = ScenarioIds.Decision,
                Messages =
                {
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = "<perceptions>Pawn is hungry. Food available in stockpile.</perceptions>" },
                },
                Examples = ThinkStrategyHelper.BuildDecisionExamples(),
                Tools = tools,
                JsonSchema = "<Action>...</Action>",
                Temperature = 0.7f,
                MaxTokens = 800,
            };
        }

        [Fact]
        public void DecisionRequest_MatchesGoldenSnapshot()
        {
            var envelope = FullDecisionRequest();

            string compact = OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800);
            // Pretty-print for human review (the on-disk artifact the maintainer reads).
            string pretty = JsonConvert.SerializeObject(
                JsonConvert.DeserializeObject(compact), Formatting.Indented);

            string path = SnapshotPath();
            if (ShouldRegenerate || !File.Exists(path))
            {
                // Write with trailing newline so the end-of-file-fixer pre-commit hook
                // does not modify the file after it is staged, which would break the lock.
                File.WriteAllText(path, pretty + "\n");
            }

            // Trim trailing whitespace so the comparison is robust to editors and hooks
            // that may add or remove a final newline.
            string expected = File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd();
            string actual = pretty.Replace("\r\n", "\n").TrimEnd();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DecisionRequest_ContainsQualityInvariants()
        {
            var envelope = FullDecisionRequest();
            string json = OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800);

            // Few-shot examples are present (BuildDecisionExamples yields 5 user/assistant pairs = 10 messages).
            Assert.Equal(10, envelope.Examples!.Count);
            // The few-shot example CONTENT actually reaches the wire JSON (locks in the dropped-examples fix).
            Assert.Contains("pawn.job.force_rest", json);
            // System guidance and the action convention are present in the wire request.
            Assert.Contains("autonomous RimWorld colonist", json);
            Assert.Contains("<Action>", json);
            // Tools are advertised, and tool_choice is set.
            Assert.Contains("pawn.move.to", json);
            Assert.Contains("\"tool_choice\"", json);
            // The non-JSON sentinel did NOT leak into a response_format block.
            Assert.DoesNotContain("\"response_format\"", json);
        }

        [Fact]
        public async System.Threading.Tasks.Task MockAIClient_CapturesLastEnvelope()
        {
            var client = new RimMind.Presentation.Tests.MockAIClient().EnqueueResponse("ok");
            var envelope = new LlmRequestEnvelope { RequestId = "cap-1", ScenarioId = ScenarioIds.Decision };

            await client.SendAsync(envelope);

            Assert.NotNull(client.LastEnvelope);
            Assert.Equal("cap-1", client.LastEnvelope!.RequestId);
        }

        [Fact]
        public void Tools_SerializeWithLowercaseOpenAIKeys()
        {
            var envelope = new LlmRequestEnvelope
            {
                RequestId = "tool-casing",
                ScenarioId = ScenarioIds.Decision,
                Messages = { new ChatMessage { Role = "user", Content = "do something" } },
                Tools = new System.Collections.Generic.List<StructuredTool>
                {
                    new StructuredTool
                    {
                        Name = "pawn.move.to",
                        Description = "Move the pawn to a target cell.",
                        Parameters = "{\"type\":\"object\",\"properties\":{\"target\":{\"type\":\"string\"}}}",
                        ToolChoice = "auto",
                    },
                },
            };

            string json = OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800);

            Assert.Contains("\"function\"", json);
            Assert.Contains("\"name\"", json);
            Assert.Contains("\"description\"", json);
            Assert.Contains("\"parameters\"", json);
            Assert.DoesNotContain("\"Function\"", json);
            Assert.DoesNotContain("\"Name\"", json);
            Assert.DoesNotContain("\"Description\"", json);
            Assert.DoesNotContain("\"Parameters\"", json);
        }

        [Fact]
        public void AssistantToolCalls_SerializeWithLowercaseOpenAIKeys()
        {
            var envelope = new LlmRequestEnvelope
            {
                RequestId = "toolcall-casing",
                ScenarioId = ScenarioIds.Decision,
                Messages =
                {
                    new ChatMessage
                    {
                        Role = "assistant",
                        Content = "",
                        ToolCalls = new System.Collections.Generic.List<ChatToolCall>
                        {
                            new ChatToolCall { Id = "call_1", Name = "pawn.move.to", Arguments = "{\"target\":\"A\"}" },
                        },
                    },
                },
            };

            string json = OpenAIRequestSerializer.BuildRequestJson(envelope, "gpt-4o-mini", 800);

            Assert.Contains("\"tool_calls\"", json);
            Assert.Contains("\"id\"", json);
            Assert.Contains("\"arguments\"", json);
            Assert.DoesNotContain("\"Id\"", json);
            Assert.DoesNotContain("\"Arguments\"", json);
            Assert.DoesNotContain("\"Function\"", json);
        }
    }
}
