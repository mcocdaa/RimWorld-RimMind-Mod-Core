using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Reflection;
using RimMind.Application.Common.Models;
using RimMind.Domain.Agent.Reflection;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Reflection
{
    internal sealed class DefaultReflectionStrategy : IReflectionStrategy
    {
        private const int ReflectionIntervalTicks = RimMindDefaults.ProactiveTickInterval; // 60000 = 1 day
        private readonly ITickProvider _tickProvider;
        private int _lastReflectionTick;

        public DefaultReflectionStrategy(ITickProvider tickProvider)
        {
            _tickProvider = tickProvider ?? throw new ArgumentNullException(nameof(tickProvider));
        }

        public bool ShouldReflect(IAgentInfo agent)
        {
            if (_lastReflectionTick == 0) return true;
            return _tickProvider.TicksGame - _lastReflectionTick >= ReflectionIntervalTicks;
        }

        public Task<Result<IReadOnlyList<ReflectionEntry>, RimMindError>> ReflectAsync(
            IAgentInfo agent, CancellationToken ct = default)
        {
            _lastReflectionTick = _tickProvider.TicksGame;

            // Skeleton: return empty list. Full AI-driven reflection will be implemented
            // when integrated with WorkingMemory and AI request pipeline.
            var entries = new List<ReflectionEntry>();
            return Task.FromResult(Result<IReadOnlyList<ReflectionEntry>, RimMindError>.Ok(entries));
        }
    }
}
