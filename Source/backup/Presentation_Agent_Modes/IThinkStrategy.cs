using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Agent.Modes;

public interface IThinkStrategy
{
    string ScenarioId { get; }

    ContextRequest BuildRequest(
        IPawnAgent agent,
        IReadOnlyList<PerceptionBufferEntry> perceptions,
        IReadOnlyList<ToolDefinition> availableTools);

    Result<AgentDecision, RimMindError> ParseDecision(
        IPawnAgent agent,
        AIResponse response);
}
