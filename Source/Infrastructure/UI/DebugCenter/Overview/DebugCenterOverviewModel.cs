namespace RimMind.Infrastructure.UI.DebugCenter.Overview
{
    public sealed class DebugCenterOverviewModel
    {
        public DebugCenterOverviewModel(
            int activeAgents,
            int pausedAgents,
            int pendingAgents,
            int errorAgents,
            int pendingRequests,
            string queueState,
            string selectedObject,
            int registeredPawnAgents,
            int registeredScopedAgents,
            int lastAgentLoopTick,
            int agentLoopFaults)
        {
            ActiveAgents = activeAgents;
            PausedAgents = pausedAgents;
            PendingAgents = pendingAgents;
            ErrorAgents = errorAgents;
            PendingRequests = pendingRequests;
            QueueState = queueState ?? string.Empty;
            SelectedObject = selectedObject ?? string.Empty;
            RegisteredPawnAgents = registeredPawnAgents;
            RegisteredScopedAgents = registeredScopedAgents;
            LastAgentLoopTick = lastAgentLoopTick;
            AgentLoopFaults = agentLoopFaults;
        }

        public int ActiveAgents { get; }

        public int PausedAgents { get; }

        public int PendingAgents { get; }

        public int ErrorAgents { get; }

        public int PendingRequests { get; }

        public string QueueState { get; }

        public string SelectedObject { get; }

        public int RegisteredPawnAgents { get; }

        public int RegisteredScopedAgents { get; }

        public int LastAgentLoopTick { get; }

        public int AgentLoopFaults { get; }

        public string AgentSummary => $"{ActiveAgents} active / {PausedAgents} paused / {PendingAgents} pending / {ErrorAgents} error";

        public string AgentLoopSummary => $"{RegisteredPawnAgents} pawn / {RegisteredScopedAgents} scoped";

        public static DebugCenterOverviewModel SnapshotFixture()
            => new(
                activeAgents: 3,
                pausedAgents: 1,
                pendingAgents: 2,
                errorAgents: 0,
                pendingRequests: 4,
                queueState: "Queue: Running",
                selectedObject: "Nickie",
                registeredPawnAgents: 3,
                registeredScopedAgents: 1,
                lastAgentLoopTick: 900,
                agentLoopFaults: 0);
    }
}
