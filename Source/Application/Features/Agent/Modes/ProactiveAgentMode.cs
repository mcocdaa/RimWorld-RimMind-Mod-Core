using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Planning;
using RimMind.Application.Common.Interfaces.Agent.Reflection;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Common.Models;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Modes;

internal sealed class ProactiveAgentMode : IAgentMode
{
    private readonly ITickProvider _tickProvider;
    private readonly IReflectionStrategy? _reflectionStrategy;
    private readonly IDailyPlanner? _dailyPlanner;

    public ProactiveAgentMode(ITickProvider tickProvider,
        IReflectionStrategy? reflectionStrategy = null,
        IDailyPlanner? dailyPlanner = null)
    {
        _tickProvider = tickProvider;
        _reflectionStrategy = reflectionStrategy;
        _dailyPlanner = dailyPlanner;
    }

    public AgentModeId ModeId => AgentModeId.Proactive;
        public string DisplayName => "Proactive";
        public string Description => "Periodically initiates decisions even without perception triggers";
        public string Id => ModeId.Value;
        public string OwnerModId => "RimMindCore";

    private const int ProactiveTickInterval = RimMindDefaults.ProactiveTickInterval;

    public bool IsApplicable(IAgentInfo agent) => agent.State == AgentState.Active;

    public bool ShouldThink(IAgentInfo agent, IReadOnlyList<PerceptionBufferEntry> perceptions)
    {
        if (perceptions.Count > 0) return true;

        var lastThinkTick = agent.LastThinkTick;
        if (lastThinkTick == null) return true;

        var ticksSinceLastThink = _tickProvider.TicksGame - lastThinkTick.Value;
        if (ticksSinceLastThink >= ProactiveTickInterval) return true;

        if (_reflectionStrategy != null && _reflectionStrategy.ShouldReflect(agent)) return true;
        if (_dailyPlanner != null && _dailyPlanner.ShouldPlan(agent)) return true;

        return false;
    }

    public IReflectionStrategy? ReflectionStrategy => _reflectionStrategy;
    public IDailyPlanner? DailyPlanner => _dailyPlanner;

    public IThinkStrategy GetThinkStrategy() => new ProactiveThinkStrategy();

    public IReadOnlyList<string> AllowedToolIds(IToolRegistry registry)
        => registry.GetAllDefinitions().Select(d => d.Id).ToList();
}

internal sealed class ProactiveThinkStrategy : IThinkStrategy
{
    public string ScenarioId => ScenarioIds.Decision;

    public ContextRequest BuildRequest(IAgentInfo agent,
        IReadOnlyList<PerceptionBufferEntry> perceptions,
        IReadOnlyList<ToolDefinition> availableTools)
    {
        var query = perceptions.Count > 0
            ? FormatPerceptions(perceptions)
            : $"Periodic self-evaluation. Current state: {SerializeAgentState(agent)}";

        return new ContextRequest
        {
            NpcId = agent.NpcId,
            Scenario = ScenarioId,
            CurrentQuery = query,
        };
    }

    public Result<AgentDecision, RimMindError> ParseDecision(IAgentInfo agent, AIResponse response)
        => new ReactiveThinkStrategy().ParseDecision(agent, response);

    private static string FormatPerceptions(IReadOnlyList<PerceptionBufferEntry> perceptions)
    {
        if (perceptions.Count == 0) return "";
        var parts = perceptions.Select(p =>
            $"[{p.PerceptionType}] {p.Content}" + (p.Importance > 0 ? $" (importance:{p.Importance:F1})" : ""));
        return string.Join("\n", parts);
    }

    private static string SerializeAgentState(IAgentInfo agent)
    {
        return $"Pawn={agent.Label}, State={agent.State}, Goals={agent.GoalCount}";
    }
}
