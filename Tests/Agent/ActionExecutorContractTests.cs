using System;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Agent
{
    /// <summary>
    /// Tests IActionExecutor contract using a stub implementation.
    /// Validates that the interface is correctly consumed by callers.
    /// </summary>
    public class ActionExecutorContractTests
    {
        [Fact]
        public void CanExecute_KnownIntent_ReturnsTrue()
        {
            var executor = new StubActionExecutor(
                knownIntents: new[] { "pawn.job.force_rest" });
            Assert.True(executor.CanExecute("pawn.job.force_rest"));
        }

        [Fact]
        public void CanExecute_UnknownIntent_ReturnsFalse()
        {
            var executor = new StubActionExecutor(
                knownIntents: new[] { "pawn.job.force_rest" });
            Assert.False(executor.CanExecute("unknown.action"));
        }

        [Fact]
        public void CanExecute_EmptyIntent_ReturnsFalse()
        {
            var executor = new StubActionExecutor();
            Assert.False(executor.CanExecute(""));
        }

        [Fact]
        public void CanExecute_NullIntent_ReturnsFalse()
        {
            var executor = new StubActionExecutor();
            Assert.False(executor.CanExecute(null!));
        }

        [Fact]
        public void ExecuteDecision_KnownIntent_ReturnsOk()
        {
            var executor = new StubActionExecutor(
                knownIntents: new[] { "pawn.job.force_rest" });
            var decision = new AgentDecision(ActionIntent: "pawn.job.force_rest");

            var result = executor.ExecuteDecision(decision, pawnId: 1);

            Assert.True(result.IsOk);
            Assert.Equal(Unit.Value, result.Value);
        }

        [Fact]
        public void ExecuteDecision_UnknownIntent_ReturnsErr()
        {
            var executor = new StubActionExecutor(
                knownIntents: Array.Empty<string>());
            var decision = new AgentDecision(ActionIntent: "unknown.action");

            var result = executor.ExecuteDecision(decision, pawnId: 1);

            Assert.True(result.IsErr);
        }

        [Fact]
        public void ExecuteDecision_NullDecision_ReturnsErr()
        {
            var executor = new StubActionExecutor();
            var result = executor.ExecuteDecision(null!, pawnId: 1);
            Assert.True(result.IsErr);
        }

        [Fact]
        public void ExecuteDecision_EmptyActionIntent_ReturnsErr()
        {
            var executor = new StubActionExecutor();
            var decision = new AgentDecision(ActionIntent: "");
            var result = executor.ExecuteDecision(decision, pawnId: 1);
            Assert.True(result.IsErr);
        }

        [Fact]
        public void ExecuteDecision_PassesPawnId_ToUnderlyingImplementation()
        {
            var executor = new StubActionExecutor(
                knownIntents: new[] { "pawn.job.force_rest" });
            var decision = new AgentDecision(ActionIntent: "pawn.job.force_rest");

            var result = executor.ExecuteDecision(decision, pawnId: 42);

            Assert.True(result.IsOk);
            Assert.Equal(42, executor.LastPawnId);
        }

        [Fact]
        public void ExecuteDecision_PawnIdZero_IsAccepted()
        {
            var executor = new StubActionExecutor(
                knownIntents: new[] { "pawn.job.force_rest" });
            var decision = new AgentDecision(ActionIntent: "pawn.job.force_rest");

            var result = executor.ExecuteDecision(decision, pawnId: 0);

            Assert.True(result.IsOk);
            Assert.Equal(0, executor.LastPawnId);
        }

        /// <summary>
        /// Stub IActionExecutor for testing the interface contract.
        /// Simulates MechanismActionExecutor behavior without Verse dependencies.
        /// </summary>
        private sealed class StubActionExecutor : IActionExecutor
        {
            private readonly string[] _knownIntents;

            public int LastPawnId { get; private set; } = -1;

            public StubActionExecutor(string[]? knownIntents = null)
            {
                _knownIntents = knownIntents ?? Array.Empty<string>();
            }

            public bool CanExecute(string actionIntent)
            {
                if (string.IsNullOrEmpty(actionIntent)) return false;
                return Array.IndexOf(_knownIntents, actionIntent) >= 0;
            }

            public Result<Unit, RimMindError> ExecuteDecision(AgentDecision decision, int pawnId)
            {
                LastPawnId = pawnId;

                if (decision == null)
                    return Result<Unit, RimMindError>.Err(RimMindErrors.Internal("AgentDecision is null"));

                if (string.IsNullOrEmpty(decision.ActionIntent))
                    return Result<Unit, RimMindError>.Err(RimMindErrors.Internal("AgentDecision.ActionIntent is empty"));

                if (!CanExecute(decision.ActionIntent))
                    return Result<Unit, RimMindError>.Err(RimMindErrors.ToolNotFound(decision.ActionIntent));

                return Result<Unit, RimMindError>.Ok(Unit.Value);
            }
        }
    }
}
