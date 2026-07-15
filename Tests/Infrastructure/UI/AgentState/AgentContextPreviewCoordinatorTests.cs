using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Context;
using RimMind.Infrastructure.UI.AgentStatePreview;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI.AgentStatePreview
{
    public class AgentContextPreviewCoordinatorTests
    {
        [Fact]
        public void Poll_LeavesPendingTaskUntouched()
        {
            var pending = new TaskCompletionSource<ContextSnapshot?>();
            var coordinator = new AgentContextPreviewCoordinator();

            coordinator.Begin(pending.Task, "loading");
            coordinator.Poll("unavailable", _ => "completed");

            Assert.Equal(AgentContextPreviewState.Pending, coordinator.State);
            Assert.Equal("loading", coordinator.Summary);
        }

        [Fact]
        public void Poll_FormatsCompletedSnapshot()
        {
            var coordinator = new AgentContextPreviewCoordinator();

            coordinator.Begin(Task.FromResult<ContextSnapshot?>(new ContextSnapshot { EstimatedTokens = 42 }), "loading");
            coordinator.Poll("unavailable", snapshot => $"tokens={snapshot.EstimatedTokens}");

            Assert.Equal(AgentContextPreviewState.Completed, coordinator.State);
            Assert.Equal("tokens=42", coordinator.Summary);
        }

        [Fact]
        public void Poll_ReportsFaultWithoutSurfacingBackgroundException()
        {
            var coordinator = new AgentContextPreviewCoordinator();

            coordinator.Begin(Task.FromException<ContextSnapshot?>(new InvalidOperationException("expected")), "loading");
            coordinator.Poll("unavailable", _ => "completed");

            Assert.Equal(AgentContextPreviewState.Faulted, coordinator.State);
            Assert.Equal("unavailable", coordinator.Summary);
        }

        [Fact]
        public void MarkUnavailable_ClearsPendingState()
        {
            var coordinator = new AgentContextPreviewCoordinator();
            coordinator.Begin(new TaskCompletionSource<ContextSnapshot?>().Task, "loading");

            coordinator.MarkUnavailable("unavailable");

            Assert.Equal(AgentContextPreviewState.Faulted, coordinator.State);
            Assert.Equal("unavailable", coordinator.Summary);
        }
    }
}
