namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IAgentTickSettings
    {
        int AgentTickInterval { get; }
        int BehaviorHistoryMax { get; set; }
        int ThinkCooldownTicks { get; }
        int MaxToolCallDepth { get; }
        int DefaultModCooldownTicks { get; set; }
    }
}
