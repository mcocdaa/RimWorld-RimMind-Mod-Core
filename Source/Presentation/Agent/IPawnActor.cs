using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using Verse.AI;

namespace RimMind.Presentation.Agent
{
    public interface IPawnActor
    {
        void Tick();
        Job? ConsumePendingJob();
        void SetPendingJob(Job job);

        /// <summary>
        /// Execute an AgentDecision by delegating to IActionExecutor.
        /// </summary>
        Result<Unit, RimMindError> ExecuteDecision(AgentDecision decision);
    }
}
