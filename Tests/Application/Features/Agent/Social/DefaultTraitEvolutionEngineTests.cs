using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Features.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Social
{
    public class DefaultTraitEvolutionEngineTests
    {
        private readonly StubTickProvider _tick = new();
        private readonly StubAgentBus _bus = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };

        private DefaultTraitEvolutionEngine CreateEngine(IPsychologyWatcher? watcher = null)
            => new(_tick, watcher, _bus);

        [Fact]
        public void ShouldEvolve_ReturnsFalse()
        {
            var engine = CreateEngine();
            Assert.False(engine.ShouldEvolve(_agent));
        }

        [Fact]
        public async Task EvaluateEvolutionAsync_ReturnsOkWithEmptyList()
        {
            var engine = CreateEngine();
            var result = await engine.EvaluateEvolutionAsync(_agent);
            Assert.True(result.IsOk);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task EvaluateEvolutionAsync_WithNullPsychologyWatcher_ReturnsOk()
        {
            var engine = CreateEngine(watcher: null);
            var result = await engine.EvaluateEvolutionAsync(_agent);
            Assert.True(result.IsOk);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task EvaluateEvolutionAsync_WithPsychologyWatcher_ReturnsOk()
        {
            var engine = CreateEngine(watcher: new StubPsychologyWatcher());
            var result = await engine.EvaluateEvolutionAsync(_agent);
            Assert.True(result.IsOk);
            Assert.Empty(result.Value);
        }
    }
}
