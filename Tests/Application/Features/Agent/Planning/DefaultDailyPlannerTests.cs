using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models;
using RimMind.Application.Features.Agent.Planning;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Planning
{
    public class DefaultDailyPlannerTests
    {
        private readonly StubTickProvider _tick = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };

        private DefaultDailyPlanner CreatePlanner()
            => new(_tick);

        // Hour 6 = ticks [15000, 17500) because TicksPerHour = 2500
        // currentHour = (currentTicks / 2500) % 24
        // currentDay = currentTicks / 60000

        [Fact]
        public void ShouldPlan_NotPlanningHour_ReturnsFalse()
        {
            // Hour 0 = ticks 0
            _tick.TicksGame = 0;
            var planner = CreatePlanner();
            Assert.False(planner.ShouldPlan(_agent));
        }

        [Fact]
        public void ShouldPlan_PlanningHourNewDay_ReturnsTrue()
        {
            // Hour 6: ticks = 6 * 2500 = 15000
            _tick.TicksGame = 15000;
            var planner = CreatePlanner();
            Assert.True(planner.ShouldPlan(_agent));
        }

        [Fact]
        public async Task ShouldPlan_PlanningHourSameDay_ReturnsFalse()
        {
            // First call at hour 6, day 0
            _tick.TicksGame = 15000;
            var planner = CreatePlanner();
            Assert.True(planner.ShouldPlan(_agent));

            // PlanAsync updates _lastPlanDay
            await planner.PlanAsync(_agent);

            // Second call at same day, hour 6 (different tick within same hour)
            _tick.TicksGame = 16000;
            Assert.False(planner.ShouldPlan(_agent));
        }

        [Fact]
        public async Task PlanAsync_UpdatesLastPlanDay()
        {
            _tick.TicksGame = 15000;
            var planner = CreatePlanner();

            // Before PlanAsync, ShouldPlan returns true
            Assert.True(planner.ShouldPlan(_agent));

            await planner.PlanAsync(_agent);

            // After PlanAsync, ShouldPlan returns false for same day
            Assert.False(planner.ShouldPlan(_agent));
        }

        [Fact]
        public async Task PlanAsync_ReturnsOkWithEmptyList()
        {
            _tick.TicksGame = 15000;
            var planner = CreatePlanner();
            var result = await planner.PlanAsync(_agent);
            Assert.True(result.IsOk);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task ShouldPlan_NextDayPlanningHour_ReturnsTrue()
        {
            _tick.TicksGame = 15000;
            var planner = CreatePlanner();
            Assert.True(planner.ShouldPlan(_agent));

            // Simulate PlanAsync being called
            _tick.TicksGame = 15000;
            await planner.PlanAsync(_agent);

            // Next day, hour 6
            _tick.TicksGame = 15000 + 60000;
            Assert.True(planner.ShouldPlan(_agent));
        }

        [Fact]
        public void Constructor_NullTickProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultDailyPlanner(null!));
        }
    }
}
