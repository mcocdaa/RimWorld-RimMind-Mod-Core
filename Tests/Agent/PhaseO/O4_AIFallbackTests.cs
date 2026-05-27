using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Events;
using Xunit;

namespace RimMind.Tests.Agent.PhaseO
{
    /// <summary>
    /// O4: AI Response Fallback — when AI returns no Action tag and no ToolCalls,
    /// the system falls back to a dialogue.free decision instead of discarding.
    /// </summary>
    public class O4_AIFallbackTests
    {
        [Fact]
        public void ParseDecisionCore_NoActionNoToolCalls_ReturnsDialogueFree()
        {
            var response = new LlmResponse
            {
                Content = "I feel uneasy about the colony's food supply."
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.False(result.Value.WantsMoreToolCalls);
        }

        [Fact]
        public void ParseDecisionCore_DialogueFree_ContainsAiResponse()
        {
            var aiText = "The weather is nice today. I should take a walk outside.";
            var response = new LlmResponse
            {
                Content = aiText
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.Equal(aiText, result.Value.Reason);
        }

        [Fact]
        public void AgentDecision_DefaultActionIntent_IsEmpty()
        {
            var decision = new AgentDecision();

            Assert.Equal("", decision.ActionIntent);
            Assert.Equal("", decision.Reason);
            Assert.Null(decision.TargetPawnId);
            Assert.Null(decision.Param);
            Assert.False(decision.WantsMoreToolCalls);
            Assert.Equal(0, decision.ToolCallRound);
        }

        [Fact]
        public void DecisionFailedEvent_Exists_AndHasCorrectEventType()
        {
            var evt = new DecisionFailedEvent("npc-1", 42, "dialogue.free", "mechanism not found");

            Assert.Equal("npc-1", evt.NpcId);
            Assert.Equal(42, evt.PawnId);
            Assert.Equal("dialogue.free", evt.ActionIntent);
            Assert.Equal("mechanism not found", evt.Reason);
            Assert.Equal(AgentBusEventType.DecisionFailed, evt.EventType);
        }

        [Fact]
        public void ParseDecisionCore_NullContent_ReturnsDialogueFreeWithEmptyReason()
        {
            var response = new LlmResponse
            {
                Content = null!
            };

            var result = ThinkStrategyHelper.ParseDecisionCore(response);

            Assert.True(result.IsOk);
            Assert.Equal("dialogue.free", result.Value.ActionIntent);
            Assert.Equal("", result.Value.Reason);
        }
    }
}
