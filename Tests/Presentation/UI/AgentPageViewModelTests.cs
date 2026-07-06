using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.AgentsPage;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public class AgentPageViewModelTests
    {
        [Fact]
        public void FromState_Active_ShowsPauseForceThinkAndOpenRequests()
        {
            var viewModel = AgentPageViewModel.FromState(
                "Mira",
                AgentState.Active,
                pendingRequests: 2,
                requestRows: 3);

            Assert.Equal("Mira", viewModel.DisplayName);
            Assert.Equal(AgentState.Active, viewModel.State);
            Assert.False(viewModel.IsPendingCreation);
            Assert.Equal(2, viewModel.PendingRequests);
            Assert.Equal(3, viewModel.RequestRows);
            Assert.Equal(
                new[]
                {
                    AgentPageAction.Pause,
                    AgentPageAction.ForceThink,
                    AgentPageAction.OpenRequests
                },
                viewModel.Actions);
            Assert.True(viewModel.CanChat);
            Assert.False(viewModel.ShowEmptyActivity);
        }

        [Fact]
        public void FromPendingCreation_ShowsCreateOnlyAndNoChat()
        {
            var viewModel = AgentPageViewModel.PendingCreation("New pawn");

            Assert.Equal("New pawn", viewModel.DisplayName);
            Assert.Equal(AgentState.Dormant, viewModel.State);
            Assert.True(viewModel.IsPendingCreation);
            Assert.Equal(0, viewModel.PendingRequests);
            Assert.Equal(0, viewModel.RequestRows);
            Assert.Equal(new[] { AgentPageAction.CreateStart }, viewModel.Actions);
            Assert.False(viewModel.CanChat);
            Assert.True(viewModel.ShowEmptyActivity);
        }

        [Fact]
        public void FromState_EmptyActivity_UsesEmptyStreamState()
        {
            var viewModel = AgentPageViewModel.FromState(
                "Quiet",
                AgentState.Paused,
                pendingRequests: 0,
                requestRows: 0);

            Assert.Equal(AgentState.Paused, viewModel.State);
            Assert.False(viewModel.IsPendingCreation);
            Assert.Equal(0, viewModel.PendingRequests);
            Assert.Equal(0, viewModel.RequestRows);
            Assert.Equal(
                new[]
                {
                    AgentPageAction.Resume,
                    AgentPageAction.ForceThink,
                    AgentPageAction.OpenRequests
                },
                viewModel.Actions);
            Assert.True(viewModel.CanChat);
            Assert.True(viewModel.ShowEmptyActivity);
        }
    }
}
