using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimMind.Domain.Enums;

namespace RimMind.Infrastructure.UI.AgentsPage
{
    public enum AgentRequestTraceStatus
    {
        Pending,
        Success,
        Error
    }

    public sealed class AgentRequestTraceRow
    {
        public AgentRequestTraceRow(
            AgentRequestTraceStatus status,
            string toolCallSummary,
            string contentSummary,
            string? error)
        {
            Status = status;
            ToolCallSummary = toolCallSummary ?? string.Empty;
            ContentSummary = contentSummary ?? string.Empty;
            Error = error;
        }

        public AgentRequestTraceStatus Status { get; }

        public string ToolCallSummary { get; }

        public string ContentSummary { get; }

        public string? Error { get; }

        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public string Summary => !string.IsNullOrWhiteSpace(ToolCallSummary)
            ? ToolCallSummary
            : ContentSummary;
    }

    public enum AgentPageAction
    {
        CreateStart,
        Activate,
        Pause,
        Resume,
        Restart,
        ForceThink,
        OpenRequests
    }

    public sealed class AgentPageViewModel
    {
        private static readonly IReadOnlyList<AgentRequestTraceRow> EmptyTraceRows =
            new ReadOnlyCollection<AgentRequestTraceRow>(new List<AgentRequestTraceRow>());

        private AgentPageViewModel(
            string displayName,
            AgentState state,
            bool isPendingCreation,
            int pendingRequests,
            int requestRows,
            IReadOnlyList<AgentRequestTraceRow> traceRows,
            IReadOnlyList<AgentPageAction> actions,
            bool canChat)
        {
            DisplayName = displayName;
            State = state;
            IsPendingCreation = isPendingCreation;
            PendingRequests = pendingRequests;
            RequestRows = requestRows;
            TraceRows = traceRows;
            Actions = actions;
            CanChat = canChat;
            ShowEmptyActivity = requestRows == 0;
        }

        public string DisplayName { get; }

        public AgentState State { get; }

        public bool IsPendingCreation { get; }

        public int PendingRequests { get; }

        public int RequestRows { get; }

        public IReadOnlyList<AgentRequestTraceRow> TraceRows { get; }

        public IReadOnlyList<AgentPageAction> Actions { get; }

        public bool CanChat { get; }

        public bool ShowEmptyActivity { get; }

        public static AgentPageViewModel PendingCreation(
            string displayName,
            int pendingRequests = 0,
            IEnumerable<AgentRequestTraceRow>? traceRows = null)
        {
            IReadOnlyList<AgentRequestTraceRow> traceRowSnapshot = SnapshotTraceRows(traceRows);
            return new AgentPageViewModel(
                displayName,
                AgentState.Dormant,
                isPendingCreation: true,
                pendingRequests,
                requestRows: traceRows == null ? 0 : traceRowSnapshot.Count,
                traceRows: traceRowSnapshot,
                actions: new ReadOnlyCollection<AgentPageAction>(
                    new List<AgentPageAction> { AgentPageAction.CreateStart }),
                canChat: false);
        }

        public static AgentPageViewModel FromState(
            string displayName,
            AgentState state,
            int pendingRequests,
            int requestRows,
            IEnumerable<AgentRequestTraceRow>? traceRows = null)
        {
            IReadOnlyList<AgentRequestTraceRow> traceRowSnapshot = SnapshotTraceRows(traceRows);
            return new AgentPageViewModel(
                displayName,
                state,
                isPendingCreation: false,
                pendingRequests,
                traceRows == null ? requestRows : traceRowSnapshot.Count,
                traceRows: traceRowSnapshot,
                actions: GetActions(state),
                canChat: CanChatFor(state));
        }

        private static IReadOnlyList<AgentRequestTraceRow> SnapshotTraceRows(
            IEnumerable<AgentRequestTraceRow>? traceRows)
        {
            if (traceRows == null)
                return EmptyTraceRows;

            return new ReadOnlyCollection<AgentRequestTraceRow>(
                new List<AgentRequestTraceRow>(traceRows));
        }

        private static IReadOnlyList<AgentPageAction> GetActions(AgentState state)
        {
            switch (state)
            {
                case AgentState.Active:
                    return new ReadOnlyCollection<AgentPageAction>(new List<AgentPageAction>
                    {
                        AgentPageAction.Pause,
                        AgentPageAction.ForceThink,
                        AgentPageAction.OpenRequests
                    });
                case AgentState.Paused:
                    return new ReadOnlyCollection<AgentPageAction>(new List<AgentPageAction>
                    {
                        AgentPageAction.Resume,
                        AgentPageAction.ForceThink,
                        AgentPageAction.OpenRequests
                    });
                case AgentState.Dormant:
                    return new ReadOnlyCollection<AgentPageAction>(
                        new List<AgentPageAction> { AgentPageAction.Activate });
                case AgentState.Terminated:
                    return new ReadOnlyCollection<AgentPageAction>(
                        new List<AgentPageAction> { AgentPageAction.Restart });
                default:
                    return new ReadOnlyCollection<AgentPageAction>(new List<AgentPageAction>());
            }
        }

        private static bool CanChatFor(AgentState state)
            => state == AgentState.Active || state == AgentState.Paused;
    }
}
