using System.Collections.Generic;
using RimMind.Domain.Enums;

namespace RimMind.Infrastructure.UI.AgentsPage
{
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
        private AgentPageViewModel(
            string displayName,
            AgentState state,
            bool isPendingCreation,
            int pendingRequests,
            int requestRows,
            IReadOnlyList<AgentPageAction> actions,
            bool canChat)
        {
            DisplayName = displayName;
            State = state;
            IsPendingCreation = isPendingCreation;
            PendingRequests = pendingRequests;
            RequestRows = requestRows;
            Actions = actions;
            CanChat = canChat;
            ShowEmptyActivity = requestRows == 0;
        }

        public string DisplayName { get; }

        public AgentState State { get; }

        public bool IsPendingCreation { get; }

        public int PendingRequests { get; }

        public int RequestRows { get; }

        public IReadOnlyList<AgentPageAction> Actions { get; }

        public bool CanChat { get; }

        public bool ShowEmptyActivity { get; }

        public static AgentPageViewModel PendingCreation(string displayName)
            => new(
                displayName,
                AgentState.Dormant,
                isPendingCreation: true,
                pendingRequests: 0,
                requestRows: 0,
                actions: new[] { AgentPageAction.CreateStart },
                canChat: false);

        public static AgentPageViewModel FromState(
            string displayName,
            AgentState state,
            int pendingRequests,
            int requestRows)
            => new(
                displayName,
                state,
                isPendingCreation: false,
                pendingRequests,
                requestRows,
                GetActions(state),
                CanChatFor(state));

        private static IReadOnlyList<AgentPageAction> GetActions(AgentState state)
        {
            switch (state)
            {
                case AgentState.Active:
                    return new[]
                    {
                        AgentPageAction.Pause,
                        AgentPageAction.ForceThink,
                        AgentPageAction.OpenRequests
                    };
                case AgentState.Paused:
                    return new[]
                    {
                        AgentPageAction.Resume,
                        AgentPageAction.ForceThink,
                        AgentPageAction.OpenRequests
                    };
                case AgentState.Dormant:
                    return new[] { AgentPageAction.Activate };
                case AgentState.Terminated:
                    return new[] { AgentPageAction.Restart };
                default:
                    return new AgentPageAction[0];
            }
        }

        private static bool CanChatFor(AgentState state)
            => state == AgentState.Active || state == AgentState.Paused;
    }
}
