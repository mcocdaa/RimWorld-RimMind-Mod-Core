using System;
using System.Collections.Generic;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Tests.Agent
{
    public class ThinkStrategyHelperTests
    {
        [Fact]
        public void ConvertToDomainTools_MapsToolDefinitions()
        {
            var defs = new List<ToolDefinition>
            {
                new() { Id = "test.tool", Description = "A test tool", ParametersSchema = "{}" }
            };

            var result = ThinkStrategyHelper.ConvertToDomainTools(defs);

            Assert.Single(result);
            Assert.Equal("test.tool", result[0].Name);
            Assert.Equal("A test tool", result[0].Description);
            Assert.Equal("{}", result[0].Parameters);
        }

        [Fact]
        public void ConvertToDomainTools_EmptyList_ReturnsEmptyList()
        {
            var result = ThinkStrategyHelper.ConvertToDomainTools(Array.Empty<ToolDefinition>());
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertToDomainTools_MultipleDefinitions_AllMapped()
        {
            var defs = new List<ToolDefinition>
            {
                new() { Id = "tool.a", Description = "Tool A", ParametersSchema = "{\"type\":\"object\"}" },
                new() { Id = "tool.b", Description = "Tool B", ParametersSchema = "{}" }
            };

            var result = ThinkStrategyHelper.ConvertToDomainTools(defs);

            Assert.Equal(2, result.Count);
            Assert.Equal("tool.a", result[0].Name);
            Assert.Equal("tool.b", result[1].Name);
        }

        [Fact]
        public void FormatPerceptions_WithEntries_ReturnsFormattedString()
        {
            var entries = new List<PerceptionBufferEntry>
            {
                new() { PerceptionType = "sight", Content = "a colonist", Importance = 0.7f }
            };

            var result = ThinkStrategyHelper.FormatPerceptions(entries);

            Assert.Contains("<perceptions>", result);
            Assert.Contains("[sight]", result);
            Assert.Contains("a colonist", result);
            Assert.Contains("importance:0.7", result);
            Assert.Contains("</perceptions>", result);
        }

        [Fact]
        public void FormatPerceptions_EmptyList_ReturnsEmptyString()
        {
            var result = ThinkStrategyHelper.FormatPerceptions(Array.Empty<PerceptionBufferEntry>());
            Assert.Equal("", result);
        }

        [Fact]
        public void FormatPerceptions_ZeroImportance_OmitsImportance()
        {
            var entries = new List<PerceptionBufferEntry>
            {
                new() { PerceptionType = "sight", Content = "test", Importance = 0f }
            };

            var result = ThinkStrategyHelper.FormatPerceptions(entries);
            Assert.DoesNotContain("importance", result);
            Assert.Contains("[sight] test", result);
            Assert.Contains("<perceptions>", result);
        }

        [Fact]
        public void FormatPerceptions_MultipleEntries_ContainsAllEntries()
        {
            var entries = new List<PerceptionBufferEntry>
            {
                new() { PerceptionType = "mood", Content = "happy", Importance = 0.3f },
                new() { PerceptionType = "health", Content = "injured", Importance = 0.8f }
            };

            var result = ThinkStrategyHelper.FormatPerceptions(entries);

            Assert.Contains("<perceptions>", result);
            Assert.Contains("[mood]", result);
            Assert.Contains("[health]", result);
            Assert.Contains("</perceptions>", result);
        }

        [Fact]
        public void ParseDecisionCore_ValidActionTag_ReturnsDecision()
        {
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\": \"force_rest\", \"reason\": \"tired\"}</Action>"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("force_rest", result.Value.ActionIntent);
            Assert.Equal("tired", result.Value.Reason);
        }

        [Fact]
        public void ParseDecisionCore_NoActionTag_ReturnsDialogueFree()
        {
            var response = new LlmResponse { Content = "no action here" };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.Equal("no action here", result.Value.Reason);
        }

        [Fact]
        public void ParseDecisionCore_WithOptionalFields_ReturnsDecision()
        {
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\": \"tend_pawn\", \"reason\": \"injured\", \"target\": \"42\", \"param\": \"{\\\"urgency\\\": \\\"high\\\"}\"}</Action>"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("tend_pawn", result.Value.ActionIntent);
            Assert.Equal("42", result.Value.TargetPawnId);
            Assert.Contains("urgency", result.Value.Param);
        }

        [Fact]
        public void ParseDecisionCore_EmptyContent_ReturnsDialogueFree()
        {
            var response = new LlmResponse { Content = "" };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecisionCore_ActionWithoutReason_DefaultsToEmptyReason()
        {
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\": \"force_rest\"}</Action>"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("force_rest", result.Value.ActionIntent);
            Assert.Equal("", result.Value.Reason);
        }
    }
}
