using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Features.Registry;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Tests.Agent
{
    /// <summary>
    /// Tests the SwitchMode logic: mode registry lookup, applicability, idempotency,
    /// and unregistered mode handling. Mirrors PawnAgent.SwitchMode behavior at the
    /// domain/application level without requiring Verse types.
    /// </summary>
    public class PawnAgentSwitchModeTests : IDisposable
    {
        private readonly ExtensionRegistry<IAgentMode> _modeRegistry;

        public PawnAgentSwitchModeTests()
        {
            _modeRegistry = new ExtensionRegistry<IAgentMode>();
            _modeRegistry.Register(new ReactiveAgentMode());
            _modeRegistry.Register(new ProactiveAgentMode(
                new StubTickProvider()));
        }

        public void Dispose() { }

        [Fact]
        public void SwitchMode_ReactiveToProactive_Succeeds()
        {
            var agent = new StubAgentInfo(AgentState.Active, AgentModeId.Reactive);
            Assert.Equal(AgentModeId.Reactive, agent.CurrentModeId);

            SwitchMode(agent, AgentModeId.Proactive);

            Assert.Equal(AgentModeId.Proactive, agent.CurrentModeId);
        }

        [Fact]
        public void SwitchMode_SameMode_IsIdempotent()
        {
            var agent = new StubAgentInfo(AgentState.Active, AgentModeId.Reactive);
            var modeBefore = agent.CurrentModeId;

            SwitchMode(agent, AgentModeId.Reactive); // Already Reactive

            Assert.Equal(modeBefore, agent.CurrentModeId);
        }

        [Fact]
        public void SwitchMode_UnregisteredMode_ThrowsInvalidOperationException()
        {
            var agent = new StubAgentInfo(AgentState.Active, AgentModeId.Reactive);

            Assert.Throws<InvalidOperationException>(() =>
                SwitchMode(agent, new AgentModeId("nonexistent.mode")));
        }

        [Fact]
        public void SwitchMode_NotActive_IsNotApplicable()
        {
            var agent = new StubAgentInfo(AgentState.Dormant, AgentModeId.Reactive);

            // Both Reactive and Proactive modes require AgentState.Active
            SwitchMode(agent, AgentModeId.Proactive);

            // Mode should NOT change because IsApplicable returns false
            Assert.Equal(AgentModeId.Reactive, agent.CurrentModeId);
        }

        [Fact]
        public void SwitchMode_ResetsLastThinkTick()
        {
            var agent = new StubAgentInfo(AgentState.Active, AgentModeId.Reactive);
            agent.LastThinkTick = 1000;

            SwitchMode(agent, AgentModeId.Proactive);

            Assert.Null(agent.LastThinkTick);
        }

        /// <summary>
        /// Replicates PawnAgent.SwitchMode logic using the mode registry.
        /// </summary>
        private void SwitchMode(StubAgentInfo agent, AgentModeId modeId)
        {
            var newMode = _modeRegistry.FindById(modeId.Value);
            if (newMode == null)
                throw new InvalidOperationException($"Mode '{modeId}' not registered");
            if (!newMode.IsApplicable(agent)) return;
            if (agent.CurrentModeId == modeId) return;

            agent.CurrentModeId = modeId;
            agent.LastThinkTick = null;
        }

        private sealed class StubTickProvider : RimMind.Application.Common.Interfaces.Abstractions.ITickProvider
        {
            public int TicksGame => 0;
        }

        /// <summary>
        /// Minimal IAgentInfo stub with mutable CurrentModeId for testing SwitchMode logic.
        /// </summary>
        private sealed class StubAgentInfo : IAgentInfo
        {
            public AgentState State { get; }
            public string NpcId => "TEST-1";
            public string Label => "TestPawn";
            public int? LastThinkTick { get; set; }
            public int GoalCount => 0;
            public AgentModeId CurrentModeId { get; set; }
            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) => Array.Empty<BehaviorRecordDto>();
            public float GetRecentSuccessRate(int count = 10) => 1.0f;

            public StubAgentInfo(AgentState state, AgentModeId currentModeId)
            {
                State = state;
                CurrentModeId = currentModeId;
            }
        }
    }
}
