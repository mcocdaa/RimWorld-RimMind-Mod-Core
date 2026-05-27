using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models;
using RimMind.Application.Features.Agent.Reflection;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Reflection
{
    public class DefaultReflectionStrategyTests
    {
        private readonly StubTickProvider _tick = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };

        private DefaultReflectionStrategy CreateStrategy()
            => new(_tick);

        [Fact]
        public void ShouldReflect_FirstTime_ReturnsTrue()
        {
            // _lastReflectionTick defaults to 0, and the check is: if (_lastReflectionTick == 0) return true
            _tick.TicksGame = 100;
            var strategy = CreateStrategy();
            Assert.True(strategy.ShouldReflect(_agent));
        }

        [Fact]
        public async Task ShouldReflect_IntervalNotEnough_ReturnsFalse()
        {
            _tick.TicksGame = 100;
            var strategy = CreateStrategy();

            // Call ReflectAsync to set _lastReflectionTick
            await strategy.ReflectAsync(_agent);

            // Interval not enough (60000 ticks needed)
            _tick.TicksGame = 30000;
            Assert.False(strategy.ShouldReflect(_agent));
        }

        [Fact]
        public async Task ShouldReflect_IntervalEnough_ReturnsTrue()
        {
            _tick.TicksGame = 100;
            var strategy = CreateStrategy();
            await strategy.ReflectAsync(_agent);

            // Interval enough: 100 + 60000 = 60100
            _tick.TicksGame = 60100;
            Assert.True(strategy.ShouldReflect(_agent));
        }

        [Fact]
        public async Task ReflectAsync_UpdatesLastReflectionTick()
        {
            _tick.TicksGame = 5000;
            var strategy = CreateStrategy();
            await strategy.ReflectAsync(_agent);

            // After ReflectAsync, ShouldReflect should return false until interval passes
            _tick.TicksGame = 5000 + 59999;
            Assert.False(strategy.ShouldReflect(_agent));

            _tick.TicksGame = 5000 + 60000;
            Assert.True(strategy.ShouldReflect(_agent));
        }

        [Fact]
        public async Task ReflectAsync_ReturnsOkWithEmptyList()
        {
            _tick.TicksGame = 100;
            var strategy = CreateStrategy();
            var result = await strategy.ReflectAsync(_agent);
            Assert.True(result.IsOk);
            Assert.Empty(result.Value);
        }

        [Fact]
        public void Constructor_NullTickProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultReflectionStrategy(null!));
        }
    }
}
