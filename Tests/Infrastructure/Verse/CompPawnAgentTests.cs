using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Registry;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Verse;
using RimMind.Presentation;
using Verse;
using Xunit;

namespace RimMind.Tests.Infrastructure.Verse
{
    public class CompPawnAgentGizmoTests : IDisposable
    {
        private readonly IExtensionRegistry<IAgentMode>? _originalModes;

        public CompPawnAgentGizmoTests()
        {
            _originalModes = RimMindAPI.Modes;
            var registry = new ExtensionRegistry<IAgentMode>();
            registry.Register(new StubAgentMode(AgentModeId.Reactive, "Reactive"));
            registry.Register(new StubAgentMode(AgentModeId.Proactive, "Proactive"));
            RimMindAPI.Modes = registry;
        }

        public void Dispose()
        {
            RimMindAPI.Modes = _originalModes;
        }

        private static CompPawnAgent CreateCompWithAgent(IAgentControl agent)
        {
            return new CompPawnAgent { Agent = agent };
        }

        /// <summary>
        /// Stub IAgentControl that supports IsActive, State, CurrentModeId, and SwitchMode.
        /// </summary>
        private sealed class StubAgentControl : IAgentControl
        {
            private AgentState _state;
            private AgentModeId _currentModeId;

            public StubAgentControl(AgentState state, AgentModeId currentModeId)
            {
                _state = state;
                _currentModeId = currentModeId;
            }

            public bool IsActive => _state == AgentState.Active;
            public AgentState State => _state;
            public AgentModeId CurrentModeId => _currentModeId;
            public IAgentMode CurrentMode
                => RimMindAPI.Modes?.FindById(_currentModeId.Value) ?? throw new InvalidOperationException();
            public bool IsPawnValid => true;
            public string GetDebugInfo() => "stub";
            public string NpcId => "TEST-1";
            public string Label => "TestPawn";
            public int? LastThinkTick { get; set; }
            public int GoalCount => 0;

            public void Tick() { }
            public bool TransitionTo(AgentState newState) { _state = newState; return true; }
            public void ForceThink() { }
            public void SwitchMode(AgentModeId modeId) => _currentModeId = modeId;
            public void Cleanup() { }
            public void Destroy() { }
            public void ResubscribeEvents() { }
            public bool RemoveGoal(string goalDescription) => false;
            public void RecordBehavior(BehaviorRecordDto record) { }
            public object? ConsumePendingJob() => null;
            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) => Array.Empty<BehaviorRecordDto>();
            public float GetRecentSuccessRate(int count = 10) => 1.0f;
        }

        /// <summary>
        /// Minimal IAgentMode stub for testing mode switch Gizmo.
        /// </summary>
        private sealed class StubAgentMode : IAgentMode
        {
            public AgentModeId ModeId { get; }
            public string DisplayName { get; }
            public string Description => $"{DisplayName} mode";
            public string Id => ModeId.Value;
            public string OwnerModId => "TestMod";

            public StubAgentMode(AgentModeId modeId, string displayName)
            {
                ModeId = modeId;
                DisplayName = displayName;
            }

            public bool IsApplicable(IAgentInfo agent) => agent.State == AgentState.Active;
            public bool ShouldThink(IAgentInfo agent, IReadOnlyList<PerceptionBufferEntry> perceptions) => true;
            public IThinkStrategy GetThinkStrategy() => throw new NotImplementedException();
            public IReadOnlyList<string> AllowedToolIds(IToolRegistry registry) => Array.Empty<string>();
        }

        [Fact]
        public void CompGetGizmosExtra_NullAgent_YieldsNoGizmos()
        {
            var comp = new CompPawnAgent { Agent = null };
            var gizmos = comp.CompGetGizmosExtra().ToList();
            Assert.Empty(gizmos);
        }

        [Fact]
        public void CompGetGizmosExtra_ActiveAgent_ContainsModeSwitchGizmo()
        {
            var agent = new StubAgentControl(AgentState.Active, AgentModeId.Reactive);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().ToList();

            // Should contain a gizmo whose label contains "Mode"
            var modeGizmo = gizmos.OfType<Command_Action>()
                .FirstOrDefault(g => g.defaultLabel.Contains("Mode"));
            Assert.NotNull(modeGizmo);
        }

        [Fact]
        public void CompGetGizmosExtra_ActiveAgent_AtLeastFiveGizmos()
        {
            // State toggle + pause + force think + dialogue + mode switch + emergency stop = 6
            var agent = new StubAgentControl(AgentState.Active, AgentModeId.Reactive);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().ToList();

            Assert.True(gizmos.Count >= 5,
                $"Expected at least 5 gizmos when active (toggle + pause + force think + dialogue + mode switch + emergency stop), got {gizmos.Count}");
        }

        [Fact]
        public void CompGetGizmosExtra_DormantAgent_NoDialogueOrModeGizmos()
        {
            var agent = new StubAgentControl(AgentState.Dormant, AgentModeId.Reactive);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();

            // Dormant agent should only have the state toggle gizmo, no dialogue or mode switch
            Assert.Single(gizmos);
            Assert.Contains("AgentState", gizmos[0].defaultLabel);
        }

        [Fact]
        public void CompGetGizmosExtra_ModeSwitchGizmo_ShowsCurrentMode()
        {
            var agent = new StubAgentControl(AgentState.Active, AgentModeId.Proactive);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();

            var modeGizmo = gizmos.FirstOrDefault(g => g.defaultLabel.Contains("Mode"));
            Assert.NotNull(modeGizmo);
            Assert.Contains("Proactive", modeGizmo.defaultLabel);
        }

        [Fact]
        public void CompGetGizmosExtra_ModeSwitchAction_OpensFloatMenu()
        {
            var agent = new StubAgentControl(AgentState.Active, AgentModeId.Reactive);
            var comp = CreateCompWithAgent(agent);

            var gizmos = comp.CompGetGizmosExtra().OfType<Command_Action>().ToList();
            var modeGizmo = gizmos.FirstOrDefault(g => g.defaultLabel.Contains("Mode"));

            Assert.NotNull(modeGizmo);
            Assert.NotNull(modeGizmo.action);

            // C-1: Mode switch now opens a FloatMenu instead of cycling
            // Invoking the action should not throw (FloatMenu creation)
            modeGizmo.action!();

            // Mode should NOT change directly — FloatMenu is shown instead
            Assert.Equal(AgentModeId.Reactive, agent.CurrentModeId);
        }
    }
}
