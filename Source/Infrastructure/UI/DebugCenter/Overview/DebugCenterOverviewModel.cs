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
            string selectedObject)
        {
            ActiveAgents = activeAgents;
            PausedAgents = pausedAgents;
            PendingAgents = pendingAgents;
            ErrorAgents = errorAgents;
            PendingRequests = pendingRequests;
            QueueState = queueState ?? string.Empty;
            SelectedObject = selectedObject ?? string.Empty;
        }

        public int ActiveAgents { get; }

        public int PausedAgents { get; }

        public int PendingAgents { get; }

        public int ErrorAgents { get; }

        public int PendingRequests { get; }

        public string QueueState { get; }

        public string SelectedObject { get; }

        public string AgentSummary => $"{ActiveAgents} active / {PausedAgents} paused / {PendingAgents} pending / {ErrorAgents} error";

        public static DebugCenterOverviewModel SnapshotFixture()
            => new(
                activeAgents: 3,
                pausedAgents: 1,
                pendingAgents: 2,
                errorAgents: 0,
                pendingRequests: 4,
                queueState: "Queue: Running",
                selectedObject: "Nickie");
    }
}
