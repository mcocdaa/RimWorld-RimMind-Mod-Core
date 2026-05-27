using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Features.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Social
{
    public class DefaultDreamGeneratorTests
    {
        private readonly StubTickProvider _tick = new();
        private readonly StubAgentBus _bus = new();
        private readonly StubSleepDetector _sleepDetector = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };

        private DefaultDreamGenerator CreateGenerator()
            => new(_tick, _sleepDetector, _bus);

        [Fact]
        public void ShouldDream_NotSleeping_ReturnsFalse()
        {
            _sleepDetector.Sleeping = false;
            var generator = CreateGenerator();
            Assert.False(generator.ShouldDream(_agent));
        }

        [Fact]
        public void ShouldDream_SleepingFirstTime_ReturnsTrue()
        {
            _sleepDetector.Sleeping = true;
            _tick.TicksGame = 60000;
            var generator = CreateGenerator();
            // First call: lastDreamTick defaults to 0, ticksSinceLastDream = 60000 >= 60000
            Assert.True(generator.ShouldDream(_agent));
        }

        [Fact]
        public void ShouldDream_SleepingIntervalNotEnough_ReturnsFalse()
        {
            _sleepDetector.Sleeping = true;
            _tick.TicksGame = 30000;
            var generator = CreateGenerator();
            // ticksSinceLastDream = 30000 < 60000
            Assert.False(generator.ShouldDream(_agent));
        }

        [Fact]
        public void ShouldDream_SleepingIntervalEnough_ReturnsTrue()
        {
            _sleepDetector.Sleeping = true;
            _tick.TicksGame = 120000;
            var generator = CreateGenerator();
            // ticksSinceLastDream = 120000 >= 60000
            Assert.True(generator.ShouldDream(_agent));
        }

        [Fact]
        public async Task GenerateDreamAsync_ReturnsErr()
        {
            var generator = CreateGenerator();
            var result = await generator.GenerateDreamAsync(_agent);
            Assert.True(result.IsErr);
        }
    }
}
