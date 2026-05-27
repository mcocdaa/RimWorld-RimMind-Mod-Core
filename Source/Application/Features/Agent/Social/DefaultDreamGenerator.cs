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

internal sealed class DefaultDreamGenerator : IDreamGenerator
{
    private readonly ITickProvider _tickProvider;
    private readonly ISleepDetector _sleepDetector;
    private readonly IAgentBus _agentBus;
    private readonly Dictionary<string, int> _lastDreamTickByNpc = new();

    public DefaultDreamGenerator(ITickProvider tickProvider, ISleepDetector sleepDetector, IAgentBus agentBus)
    {
        _tickProvider = tickProvider;
        _sleepDetector = sleepDetector;
        _agentBus = agentBus;
    }

    public bool ShouldDream(IAgentInfo agent)
    {
        if (!_sleepDetector.IsSleeping(agent)) return false;
        var lastDreamTick = _lastDreamTickByNpc.TryGetValue(agent.NpcId, out var t) ? t : 0;
        var ticksSinceLastDream = _tickProvider.TicksGame - lastDreamTick;
        return ticksSinceLastDream >= 60000;
    }

    public Task<Result<DreamEntry, RimMindError>> GenerateDreamAsync(
        IAgentInfo agent, CancellationToken ct = default)
        => Task.FromResult(Result<DreamEntry, RimMindError>.Err(
            RimMindErrors.NotImplemented("DreamGenerator")));
}
