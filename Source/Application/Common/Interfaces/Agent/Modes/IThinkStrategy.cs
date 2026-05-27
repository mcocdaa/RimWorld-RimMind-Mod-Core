using System.Collections.Generic;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Agent.Modes;

public interface IThinkStrategy
{
    string ScenarioId { get; }

    LlmRequestEnvelope BuildEnvelope(
        IAgentInfo agent,
        IReadOnlyList<PerceptionBufferEntry> perceptions,
        IReadOnlyList<ToolDefinition> availableTools);

    Result<AgentDecision, RimMindError> ParseDecision(
        IAgentInfo agent,
        LlmResponse response,
        IReadOnlyList<ToolResult>? toolCallResults = null);
}
