namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IPawnAgentFactory
    {
        object Create(object pawn, object eventBus);
        void SerializeAgent(ref object? agent, string label);
    }
}
