using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;

namespace RimMind.Application.Common.Interfaces.Agent.Modes;

public interface IAgentMode : IExtension
{
    AgentModeId ModeId { get; }
    string DisplayName { get; }
    string Description { get; }

    bool IsApplicable(IPawnAgent agent);

    bool ShouldThink(IPawnAgent agent, IReadOnlyList<PerceptionBufferEntry> perceptions);

    IThinkStrategy GetThinkStrategy();

    IReadOnlyList<string> AllowedToolIds(IToolRegistry registry);
}
