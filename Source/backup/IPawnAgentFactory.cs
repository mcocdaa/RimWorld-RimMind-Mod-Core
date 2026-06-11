using RimMind.Application.Common.Interfaces;
using Verse;

namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IPawnAgentFactory
    {
        IPawnAgent Create(Pawn pawn, IAgentBus agentBus);
        void SerializeAgent(ref IPawnAgent? agent, string label);
    }
}
