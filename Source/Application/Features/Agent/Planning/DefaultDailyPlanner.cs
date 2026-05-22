using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Planning;
using RimMind.Application.Common.Models;
using RimMind.Domain.Agent.Planning;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Planning
{
    internal sealed class DefaultDailyPlanner : IDailyPlanner
    {
        private const int PlanningHour = 6; // 6:00 AM game time
        private const int TicksPerHour = 2500; // RimWorld: 60000 ticks/day, 24 hours/day
        private readonly ITickProvider _tickProvider;
        private int _lastPlanDay = -1;

        public DefaultDailyPlanner(ITickProvider tickProvider)
        {
            _tickProvider = tickProvider ?? throw new ArgumentNullException(nameof(tickProvider));
        }

        public bool ShouldPlan(IAgentInfo agent)
        {
            var currentTicks = _tickProvider.TicksGame;
            var currentHour = (currentTicks / TicksPerHour) % 24;
            var currentDay = currentTicks / RimMindDefaults.ProactiveTickInterval;

            if (currentHour != PlanningHour) return false;
            if (currentDay == _lastPlanDay) return false;

            return true;
        }

        public Task<Result<IReadOnlyList<ScheduleBlock>, RimMindError>> PlanAsync(
            IAgentInfo agent, CancellationToken ct = default)
        {
            var currentTicks = _tickProvider.TicksGame;
            _lastPlanDay = currentTicks / RimMindDefaults.ProactiveTickInterval;

            // Skeleton: return empty list. Full AI-driven planning will be implemented
            // when integrated with WorkingMemory and AI request pipeline.
            var blocks = new List<ScheduleBlock>();
            return Task.FromResult(Result<IReadOnlyList<ScheduleBlock>, RimMindError>.Ok(blocks));
        }
    }
}
