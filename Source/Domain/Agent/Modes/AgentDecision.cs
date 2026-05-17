namespace RimMind.Domain.Agent.Modes
{
    public sealed record AgentDecision(
        string ActionIntent = "",
        string Reason = "",
        string? TargetPawnId = null,
        string? Param = null);
}
