using System.Threading.Tasks;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.UI.AgentFlow;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public class AgentFlowAsyncCoordinatorTests
    {
        [Fact]
        public void ContextBuild_Polls_Only_After_The_Task_Completes()
        {
            var coordinator = new AgentFlowAsyncCoordinator();
            var completion = new TaskCompletionSource<ContextSnapshot?>();

            coordinator.BeginContextBuild(completion.Task);

            Assert.False(coordinator.PollContextBuild(out var pendingSnapshot, out var pendingError));
            Assert.Null(pendingSnapshot);
            Assert.Null(pendingError);

            var expected = new ContextSnapshot { NpcId = "NPC-1" };
            completion.SetResult(expected);

            Assert.True(coordinator.PollContextBuild(out var completedSnapshot, out var completedError));
            Assert.Same(expected, completedSnapshot);
            Assert.Null(completedError);
        }

        [Fact]
        public void MechanismExecution_Polls_Completed_Result_Without_Blocking()
        {
            var coordinator = new AgentFlowAsyncCoordinator();
            var completion = new TaskCompletionSource<Result<bool, RimMindError>>();

            coordinator.BeginMechanismExecution(completion.Task);

            Assert.False(coordinator.PollMechanismExecution(out var pendingResult, out var pendingError));
            Assert.Null(pendingResult);
            Assert.Null(pendingError);

            completion.SetResult(Result<bool, RimMindError>.Ok(true));

            Assert.True(coordinator.PollMechanismExecution(out var completedResult, out var completedError));
            Assert.True(completedResult!.Value.IsOk);
            Assert.Null(completedError);
        }

        [Fact]
        public void MechanismExecution_Remains_Pending_Until_The_Result_Is_Polled()
        {
            var coordinator = new AgentFlowAsyncCoordinator();
            var completion = new TaskCompletionSource<Result<bool, RimMindError>>();

            coordinator.BeginMechanismExecution(completion.Task);

            Assert.True(coordinator.HasPendingMechanismExecution);

            completion.SetResult(Result<bool, RimMindError>.Ok(true));
            Assert.True(coordinator.HasPendingMechanismExecution);
            Assert.True(coordinator.PollMechanismExecution(out _, out _));
            Assert.False(coordinator.HasPendingMechanismExecution);
        }

        [Fact]
        public void ResetContextBuild_Discards_Only_The_Stale_Context_Work()
        {
            var coordinator = new AgentFlowAsyncCoordinator();
            var contextCompletion = new TaskCompletionSource<ContextSnapshot?>();
            var executionCompletion = new TaskCompletionSource<Result<bool, RimMindError>>();
            coordinator.BeginContextBuild(contextCompletion.Task);
            coordinator.BeginMechanismExecution(executionCompletion.Task);

            coordinator.ResetContextBuild();

            contextCompletion.SetResult(new ContextSnapshot { NpcId = "NPC-1" });
            executionCompletion.SetResult(Result<bool, RimMindError>.Ok(true));
            Assert.False(coordinator.PollContextBuild(out _, out _));
            Assert.True(coordinator.PollMechanismExecution(out var result, out _));
            Assert.True(result!.Value.IsOk);
        }

        [Fact]
        public void MechanismExecution_Preserves_Its_Scheduled_Target_Context_After_Scope_Switch()
        {
            var coordinator = new AgentFlowAsyncCoordinator();
            var completion = new TaskCompletionSource<Result<bool, RimMindError>>();
            var scheduledContext = new AgentFlowExecutionContext(
                targetGeneration: 3,
                scope: "Pawn",
                targetId: "NPC-17",
                mechanismId: "pawn.job.force_rest",
                operation: MechanismOperationType.Set);

            coordinator.BeginMechanismExecution(completion.Task, scheduledContext);

            Assert.True(coordinator.HasPendingMechanismExecutionForGeneration(3));
            Assert.False(coordinator.HasPendingMechanismExecutionForGeneration(4));

            completion.SetResult(Result<bool, RimMindError>.Ok(true));

            Assert.True(coordinator.PollMechanismExecution(out var executionCompletion));
            Assert.NotNull(executionCompletion);
            Assert.Same(scheduledContext, executionCompletion!.Context);
            Assert.True(executionCompletion.Result!.Value.IsOk);
        }
    }
}
