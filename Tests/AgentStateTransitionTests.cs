using RimMind.Core.Agent;
using Xunit;

namespace RimMind.Core.Tests
{
    public class AgentStateTransitionTests
    {
        [Theory]
        [InlineData(AgentState.Dormant, AgentState.Active)]
        [InlineData(AgentState.Dormant, AgentState.Terminated)]
        [InlineData(AgentState.Active, AgentState.Paused)]
        [InlineData(AgentState.Active, AgentState.Dormant)]
        [InlineData(AgentState.Active, AgentState.Terminated)]
        [InlineData(AgentState.Paused, AgentState.Active)]
        [InlineData(AgentState.Paused, AgentState.Dormant)]
        [InlineData(AgentState.Paused, AgentState.Terminated)]
        public void CanTransition_ValidTransitions_ReturnTrue(AgentState from, AgentState to)
        {
            Assert.True(AgentStateTransition.CanTransition(from, to));
        }

        [Theory]
        [InlineData(AgentState.Dormant, AgentState.Dormant)]
        [InlineData(AgentState.Dormant, AgentState.Paused)]
        [InlineData(AgentState.Active, AgentState.Active)]
        [InlineData(AgentState.Paused, AgentState.Paused)]
        [InlineData(AgentState.Terminated, AgentState.Dormant)]
        [InlineData(AgentState.Terminated, AgentState.Active)]
        [InlineData(AgentState.Terminated, AgentState.Paused)]
        [InlineData(AgentState.Terminated, AgentState.Terminated)]
        public void CanTransition_InvalidTransitions_ReturnFalse(AgentState from, AgentState to)
        {
            Assert.False(AgentStateTransition.CanTransition(from, to));
        }

        [Fact]
        public void Terminated_IsTerminalState()
        {
            Assert.False(AgentStateTransition.CanTransition(AgentState.Terminated, AgentState.Dormant));
            Assert.False(AgentStateTransition.CanTransition(AgentState.Terminated, AgentState.Active));
            Assert.False(AgentStateTransition.CanTransition(AgentState.Terminated, AgentState.Paused));
            Assert.False(AgentStateTransition.CanTransition(AgentState.Terminated, AgentState.Terminated));
        }

        [Fact]
        public void Dormant_CanOnlyGoToActiveOrTerminated()
        {
            Assert.True(AgentStateTransition.CanTransition(AgentState.Dormant, AgentState.Active));
            Assert.True(AgentStateTransition.CanTransition(AgentState.Dormant, AgentState.Terminated));
            Assert.False(AgentStateTransition.CanTransition(AgentState.Dormant, AgentState.Paused));
        }

        [Fact]
        public void Active_CanGoToPausedDormantTerminated()
        {
            Assert.True(AgentStateTransition.CanTransition(AgentState.Active, AgentState.Paused));
            Assert.True(AgentStateTransition.CanTransition(AgentState.Active, AgentState.Dormant));
            Assert.True(AgentStateTransition.CanTransition(AgentState.Active, AgentState.Terminated));
        }
    }
}
