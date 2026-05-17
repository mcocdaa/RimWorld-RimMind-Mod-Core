using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Application.Features.Agent.Modes;

internal sealed class ProactiveAgentMode : IAgentMode
{
    public AgentModeId ModeId => AgentModeId.Proactive;
    public string DisplayName => "Proactive";
    public string Description => "Periodically initiates decisions even without perception triggers";
    public string Id => ModeId.Value;

    private const int ProactiveTickInterval = 60000;

    public bool IsApplicable(IPawnAgent agent) => agent.State == AgentState.Active;

    public bool ShouldThink(IPawnAgent agent, IReadOnlyList<PerceptionBufferEntry> perceptions)
    {
        if (perceptions.Count > 0) return true;

        var lastThinkTick = agent.LastThinkTick;
        if (lastThinkTick == null) return true;

        var ticksSinceLastThink = Find.TickManager.TicksGame - lastThinkTick.Value;
        return ticksSinceLastThink >= ProactiveTickInterval;
    }

    public IThinkStrategy GetThinkStrategy() => new ProactiveThinkStrategy();

    public IReadOnlyList<string> AllowedToolIds(IToolRegistry registry)
        => registry.GetAllDefinitions().Select(d => d.Id).ToList();
}

internal sealed class ProactiveThinkStrategy : IThinkStrategy
{
    public string ScenarioId => ScenarioIds.Decision;

    public ContextRequest BuildRequest(IPawnAgent agent,
        IReadOnlyList<PerceptionBufferEntry> perceptions,
        IReadOnlyList<ToolDefinition> availableTools)
    {
        var query = perceptions.Count > 0
            ? FormatPerceptions(perceptions)
            : $"Periodic self-evaluation. Current state: {SerializeAgentState(agent)}";

        return new ContextRequest
        {
            NpcId = agent.Identity.NpcId,
            Scenario = ScenarioId,
            CurrentQuery = query,
        };
    }

    public Result<AgentDecision, RimMindError> ParseDecision(IPawnAgent agent, AIResponse response)
        => new ReactiveThinkStrategy().ParseDecision(agent, response);

    private static string FormatPerceptions(IReadOnlyList<PerceptionBufferEntry> perceptions)
    {
        if (perceptions.Count == 0) return "";
        var parts = perceptions.Select(p =>
            $"[{p.PerceptionType}] {p.Content}" + (p.Importance > 0 ? $" (importance:{p.Importance:F1})" : ""));
        return string.Join("\n", parts);
    }

    private static string SerializeAgentState(IPawnAgent agent)
    {
        var pawn = agent.Pawn;
        var goals = agent.GoalStack;
        return $"Pawn={pawn?.Label ?? "null"}, State={agent.State}, Goals={goals?.TotalCount ?? 0}";
    }
}
