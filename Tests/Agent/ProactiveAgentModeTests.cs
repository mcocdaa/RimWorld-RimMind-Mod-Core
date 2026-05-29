using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
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
    public class ProactiveAgentModeTests
    {
        private readonly ProactiveAgentMode _mode;

        public ProactiveAgentModeTests()
        {
            _mode = new ProactiveAgentMode(new TestTickProvider());
        }

        [Fact]
        public void ModeId_ReturnsProactive()
        {
            Assert.Equal(AgentModeId.Proactive, _mode.ModeId);
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
        public void ShouldThink_NoPerceptions_NoLastThink_ReturnsTrue()
        {
            var agent = new TestAgentInfo { State = AgentState.Active, LastThinkTick = null };
            Assert.True(_mode.ShouldThink(agent, Array.Empty<PerceptionBufferEntry>()));
        }

        [Fact]
        public void AllowedToolIds_ReturnsAllTools()
        {
            var registry = new TestToolRegistry(new[]
            {
                "pawn.job.force_rest",
                "planning.daily_plan",
                "dream.night_dream",
                "reflect.self_review",
                "trait_evolution.evolve",
            });

            var allowed = _mode.AllowedToolIds(registry);

            Assert.Equal(5, allowed.Count);
            Assert.Contains("planning.daily_plan", allowed);
            Assert.Contains("dream.night_dream", allowed);
        }

        [Fact]
        public void GetThinkStrategy_ReturnsProactiveThinkStrategy()
        {
            var strategy = _mode.GetThinkStrategy();
            Assert.IsType<ProactiveThinkStrategy>(strategy);
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

        private class TestTickProvider : ITickProvider
        {
            public int TicksGame => 100000;
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
