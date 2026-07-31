using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Social;

public sealed class DefaultTraitEvolutionEngine : ITraitEvolutionEngine
{
    private readonly ITickProvider _tickProvider;
    private readonly IPsychologyWatcher? _psychologyWatcher;
    private readonly IAgentBus _agentBus;

    public DefaultTraitEvolutionEngine(ITickProvider tickProvider, IPsychologyWatcher? psychologyWatcher, IAgentBus agentBus)
    {
        _tickProvider = tickProvider;
        _psychologyWatcher = psychologyWatcher;
        _agentBus = agentBus;
    }

    public bool ShouldEvolve(IAgentInfo agent) => false;

    public Task<Result<IReadOnlyList<TraitEvolutionRecord>, RimMindError>> EvaluateEvolutionAsync(
        IAgentInfo agent, CancellationToken ct = default)
        => Task.FromResult(Result<IReadOnlyList<TraitEvolutionRecord>, RimMindError>.Ok(
            Array.Empty<TraitEvolutionRecord>()));
}
