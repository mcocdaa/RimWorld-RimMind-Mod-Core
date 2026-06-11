using RimMind.Domain.Enums;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public sealed record AgentListItem(
        string Id,
        string Label,
        AgentState State,
        bool IsPendingCreation)
    {
        public static AgentListItem ExistingPawn(string id, string label, AgentState state)
            => new(id, label, state, IsPendingCreation: false);

        public static AgentListItem PendingPawn(string id, string label)
            => new(id, label, AgentState.Dormant, IsPendingCreation: true);
    }
}
