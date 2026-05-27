using System.Reflection;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Tests.Agent.PhaseO
{
    /// <summary>
    /// O2: Agentic Loop fix — WantsMoreToolCalls inference logic.
    /// Verifies AgentDecision fields, RimMindDefaults constants,
    /// IThinkStrategy contract, and ParseDecisionCore behavior.
    /// </summary>
    public class O2_AgenticLoopTests
    {
        // === AgentDecision field existence (ArchTest-style via reflection) ===

        [Fact]
        public void AgentDecision_Has_WantsMoreToolCalls_Field()
        {
            var prop = typeof(AgentDecision).GetProperty("WantsMoreToolCalls");
            Assert.NotNull(prop);
            Assert.Equal(typeof(bool), prop.PropertyType);

            var decision = new AgentDecision();
            Assert.False(decision.WantsMoreToolCalls);
        }

        [Fact]
        public void AgentDecision_Has_ToolCallRound_Field()
        {
            var prop = typeof(AgentDecision).GetProperty("ToolCallRound");
            Assert.NotNull(prop);
            Assert.Equal(typeof(int), prop.PropertyType);

            var decision = new AgentDecision();
            Assert.Equal(0, decision.ToolCallRound);
        }

        // === RimMindDefaults constant ===

        [Fact]
        public void RimMindDefaults_DefaultMaxToolCallDepth_IsPositive()
        {
            Assert.True(RimMindDefaults.DefaultMaxToolCallDepth > 0,
                $"DefaultMaxToolCallDepth must be > 0, got {RimMindDefaults.DefaultMaxToolCallDepth}");
        }

        // === IThinkStrategy contract ===

        [Fact]
        public void IThinkStrategy_Defines_ParseDecision()
        {
            var method = typeof(IThinkStrategy).GetMethod("ParseDecision");
            Assert.NotNull(method);

            var parameters = method.GetParameters();
            Assert.True(parameters.Length >= 2,
                "ParseDecision must have at least 2 parameters (agent, response)");

            // Verify the response parameter is LlmResponse
            Assert.Equal(typeof(LlmResponse), parameters[1].ParameterType);
        }

        // === ParseDecisionCore WantsMoreToolCalls inference ===

        [Fact]
        public void ParseDecisionCore_ToolCallsWithoutAction_SetsWantsMoreToolCalls()
        {
            var response = new LlmResponse
            {
                Content = "I need to check the weather first.",
                ToolCallsJson = "[{\"id\":\"tc-1\",\"function\":{\"name\":\"get_weather\",\"arguments\":\"{}\"}}]"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.True(result.Value.WantsMoreToolCalls);
            Assert.Equal("", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecisionCore_ActionWithTag_SetsWantsMoreToolCallsFalse()
        {
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\":\"force_rest\",\"reason\":\"tired\"}</Action>"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.False(result.Value.WantsMoreToolCalls);
            Assert.Equal("force_rest", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecisionCore_ActionAndToolCalls_ActionTakesPriority()
        {
            var response = new LlmResponse
            {
                Content = "<Action>{\"action\":\"investigate\",\"reason\":\"done checking\"}</Action>",
                ToolCallsJson = "[{\"id\":\"tc-1\",\"function\":{\"name\":\"get_weather\",\"arguments\":\"{}\"}}]"
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.False(result.Value.WantsMoreToolCalls);
            Assert.Equal("investigate", result.Value.ActionIntent);
        }

        [Fact]
        public void ParseDecisionCore_NoActionNoToolCalls_ReturnsDialogueFreeFallback()
        {
            var response = new LlmResponse
            {
                Content = "I am thinking about what to do."
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.Equal("I am thinking about what to do.", result.Value.Reason);
            Assert.False(result.Value.WantsMoreToolCalls);
        }

        [Fact]
        public void ParseDecisionCore_EmptyToolCallsJson_NoAction_ReturnsDialogueFreeFallback()
        {
            var response = new LlmResponse
            {
                Content = "No action here",
                ToolCallsJson = ""
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.Equal("No action here", result.Value.Reason);
        }
    }
}
