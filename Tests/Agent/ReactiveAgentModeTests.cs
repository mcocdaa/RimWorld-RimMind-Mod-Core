using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Tests.Agent
{
    public class ReactiveAgentModeTests
    {
        private readonly ReactiveAgentMode _mode = new();

        [Fact]
        public void ModeId_ReturnsReactive()
        {
            Assert.Equal(AgentModeId.Reactive, _mode.ModeId);
        }

        [Fact]
        public void ShouldThink_WithPerceptions_ReturnsTrue()
        {
            var agent = new TestAgentInfo { State = AgentState.Active };
            var perceptions = new List<PerceptionBufferEntry>
            {
                new() { PerceptionType = "sight", Content = "test", Importance = 0.5f }
            };
            Assert.True(_mode.ShouldThink(agent, perceptions));
        }

        [Fact]
        public void ShouldThink_WithNoPerceptions_ReturnsFalse()
        {
            var agent = new TestAgentInfo { State = AgentState.Active };
            Assert.False(_mode.ShouldThink(agent, Array.Empty<PerceptionBufferEntry>()));
        }

        [Fact]
        public void AllowedToolIds_ExcludesProactivePrefixes()
        {
            var registry = new TestToolRegistry(new[]
            {
                "pawn.job.force_rest",
                "planning.daily_plan",
                "dream.night_dream",
                "reflect.self_review",
                "trait_evolution.evolve",
                "pawn.mood.set_mood",
            });

            var allowed = _mode.AllowedToolIds(registry);

            Assert.DoesNotContain("planning.daily_plan", allowed);
            Assert.DoesNotContain("dream.night_dream", allowed);
            Assert.DoesNotContain("reflect.self_review", allowed);
            Assert.DoesNotContain("trait_evolution.evolve", allowed);
            Assert.Contains("pawn.job.force_rest", allowed);
            Assert.Contains("pawn.mood.set_mood", allowed);
        }

        [Fact]
        public void GetThinkStrategy_ReturnsReactiveThinkStrategy()
        {
            var strategy = _mode.GetThinkStrategy();
            Assert.IsType<ReactiveThinkStrategy>(strategy);
        }

        [Fact]
        public void IsApplicable_ActiveState_ReturnsTrue()
        {
            var agent = new TestAgentInfo { State = AgentState.Active };
            Assert.True(_mode.IsApplicable(agent));
        }

        [Fact]
        public void IsApplicable_DormantState_ReturnsFalse()
        {
            var agent = new TestAgentInfo { State = AgentState.Dormant };
            Assert.False(_mode.IsApplicable(agent));
        }

        private class TestAgentInfo : IAgentInfo
        {
            public string NpcId => "test-npc";
            public string Label => "Test";
            public AgentState State { get; set; } = AgentState.Active;
            public int? LastThinkTick { get; set; } = null;
            public int GoalCount => 0;
            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) => Array.Empty<BehaviorRecordDto>();
            public float GetRecentSuccessRate(int count = 10) => 1.0f;
        }

        private class TestToolRegistry : IToolRegistry
        {
            private readonly List<ToolDefinition> _defs;
            public TestToolRegistry(IEnumerable<string> ids)
            {
                _defs = ids.Select(id => new ToolDefinition { Id = id, Description = id, ParametersSchema = "" }).ToList();
            }
            public IReadOnlyList<ToolDefinition> GetAllDefinitions() => _defs;
            public IToolHandler? FindById(string id) => null;
            public void Register(IToolHandler handler) { }
            public bool Unregister(string toolId) => false;
            public IReadOnlyList<IToolHandler> All => Array.Empty<IToolHandler>();
        }
    }
}
