namespace RimMind.Application.Common.Interfaces.Agent
{
    public interface IPawnAgentFactory
    {
        object Create(object pawn, object eventBus);
    }
}
