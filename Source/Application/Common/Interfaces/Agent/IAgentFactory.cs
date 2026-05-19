namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IAgentFactory
    {
        IAgentControl CreateAgent(object pawn, IAgentBus agentBus);
        void SerializeAgent(ref IAgentControl? agent, string label);
    }
}
