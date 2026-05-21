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
using RimMind.Application.Features.Json;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent.Modes;

internal sealed class ReactiveAgentMode : IAgentMode
{
    public AgentModeId ModeId => AgentModeId.Reactive;
        public string DisplayName => "Reactive";
        public string Description => "Responds to perception events only";
        public string Id => ModeId.Value;
        public string OwnerModId => "RimMindCore";

    public bool IsApplicable(IAgentInfo agent)
        => agent.State == AgentState.Active;

    public bool ShouldThink(IAgentInfo agent, IReadOnlyList<PerceptionBufferEntry> perceptions)
        => perceptions.Count > 0;

    public IThinkStrategy GetThinkStrategy() => new ReactiveThinkStrategy();

    public IReadOnlyList<string> AllowedToolIds(IToolRegistry registry)
        => registry.GetAllDefinitions().Select(d => d.Id).ToList();
}

internal sealed class ReactiveThinkStrategy : IThinkStrategy
{
    public string ScenarioId => ScenarioIds.Decision;

    public ContextRequest BuildRequest(IAgentInfo agent,
        IReadOnlyList<PerceptionBufferEntry> perceptions,
        IReadOnlyList<ToolDefinition> availableTools)
    {
        var query = FormatPerceptions(perceptions);
        return new ContextRequest
        {
            NpcId = agent.NpcId,
            Scenario = ScenarioId,
            CurrentQuery = query,
        };
    }

    public Result<AgentDecision, RimMindError> ParseDecision(IAgentInfo agent, AIResponse response)
    {
        var json = JsonTagExtractor.Extract<ActionJson>(response.Content, "Action");
        if (json == null)
            return Result<AgentDecision, RimMindError>.Err(
                RimMindErrors.Internal("No <Action> tag in AI response"));

        return Result<AgentDecision, RimMindError>.Ok(new AgentDecision
        {
            ActionIntent = json.action,
            Reason = json.reason ?? "",
            TargetPawnId = json.target,
            Param = json.param,
        });
    }

    private static string FormatPerceptions(IReadOnlyList<PerceptionBufferEntry> perceptions)
    {
        if (perceptions.Count == 0) return "";
        var parts = perceptions.Select(p =>
            $"[{p.PerceptionType}] {p.Content}" + (p.Importance > 0 ? $" (importance:{p.Importance:F1})" : ""));
        return string.Join("\n", parts);
    }

    private class ActionJson
    {
#pragma warning disable CS0649
        public string action = "";
        public string? reason;
        public string? target;
        public string? param;
#pragma warning restore CS0649
    }
}
