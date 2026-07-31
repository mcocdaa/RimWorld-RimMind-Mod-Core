using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Social;

public sealed class DefaultSocialEventOrganizer : ISocialEventOrganizer
{
    private readonly ITickProvider _tickProvider;
    private readonly IAgentBus _agentBus;

    public DefaultSocialEventOrganizer(ITickProvider tickProvider, IAgentBus agentBus)
    {
        _tickProvider = tickProvider;
        _agentBus = agentBus;
    }

    public bool ShouldOrganize(IAgentInfo agent) => false;

    public Task<Result<SocialEventPlan, RimMindError>> OrganizeAsync(IAgentInfo agent, CancellationToken ct = default)
        => Task.FromResult(Result<SocialEventPlan, RimMindError>.Err(
            RimMindErrors.NotImplemented("SocialEventOrganizer")));

    public IReadOnlyList<SocialEventPlan> GetPendingEvents() => Array.Empty<SocialEventPlan>();
    public void MarkEventExecuted(string eventId) { }
}
