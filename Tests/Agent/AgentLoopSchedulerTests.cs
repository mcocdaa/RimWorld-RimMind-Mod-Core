using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Tests.Agent
{
    public sealed class AgentLoopSchedulerTests
    {
        [Fact]
        public void Tick_SameGameTick_TicksEachRegistrationExactlyOnce()
        {
            var scheduler = new AgentLoopScheduler();
            var pawn = new StubAgentControl();
            var scoped = new StubAgentControl();
            scheduler.Register(AgentLoopKeys.ForPawn(11), AgentLoopKind.Pawn, pawn);
            scheduler.Register(AgentLoopKeys.ForScoped("world:alpha"), AgentLoopKind.Scoped, scoped);

            scheduler.Tick(100);
            scheduler.Tick(100);

            Assert.Equal(1, pawn.TickCount);
            Assert.Equal(1, scoped.TickCount);
        }

        [Fact]
        public void Tick_DifferentSubsequentGameTick_TicksAgain()
        {
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl();
            scheduler.Register(AgentLoopKeys.ForPawn(12), AgentLoopKind.Pawn, agent);

            scheduler.Tick(100);
            scheduler.Tick(101);

            Assert.Equal(2, agent.TickCount);
        }

        [Fact]
        public void Register_ReplacementAtStableKey_TicksOnlyReplacementAndFindReturnsIt()
        {
            var scheduler = new AgentLoopScheduler();
            var key = AgentLoopKeys.ForPawn(13);
            var original = new StubAgentControl();
            var replacement = new StubAgentControl();
            scheduler.Register(key, AgentLoopKind.Pawn, original);

            Assert.True(scheduler.Register(key, AgentLoopKind.Pawn, replacement));
            scheduler.Tick(200);

            Assert.Equal(0, original.TickCount);
            Assert.Equal(1, replacement.TickCount);
            Assert.Same(replacement, scheduler.Find(key));
        }

        [Fact]
        public void Register_SameInstanceAndKind_ReturnsFalse()
        {
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl();
            var key = AgentLoopKeys.ForPawn(14);

            Assert.True(scheduler.Register(key, AgentLoopKind.Pawn, agent));
            Assert.False(scheduler.Register(key, AgentLoopKind.Pawn, agent));
        }

        [Fact]
        public void Register_BlankKeyOrNullAgent_Throws()
        {
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl();

            Assert.Throws<ArgumentException>(() => scheduler.Register(" ", AgentLoopKind.Pawn, agent));
            Assert.Throws<ArgumentNullException>(() => scheduler.Register("pawn:15", AgentLoopKind.Pawn, null!));
        }

        [Fact]
        public void Unregister_UnknownKey_ReturnsFalse()
        {
            var scheduler = new AgentLoopScheduler();

            Assert.False(scheduler.Unregister("missing"));
        }

        [Fact]
        public void AgentLoopKeys_PawnAndScopedKeysCannotCollide()
        {
            var pawnKey = AgentLoopKeys.ForPawn(42);
            var scopedKey = AgentLoopKeys.ForScoped("42");

            Assert.Equal("pawn:42", pawnKey);
            Assert.Equal("scope:42", scopedKey);
            Assert.NotEqual(pawnKey, scopedKey);
        }

        [Fact]
        public void Tick_SelfUnregister_DoesNotBreakIteration()
        {
            var scheduler = new AgentLoopScheduler();
            var selfKey = AgentLoopKeys.ForPawn(16);
            var selfRemoving = new StubAgentControl(onTick: () => scheduler.Unregister(selfKey));
            var participant = new StubAgentControl();
            scheduler.Register(selfKey, AgentLoopKind.Pawn, selfRemoving);
            scheduler.Register(AgentLoopKeys.ForPawn(17), AgentLoopKind.Pawn, participant);

            scheduler.Tick(300);

            Assert.Equal(1, selfRemoving.TickCount);
            Assert.Equal(1, participant.TickCount);
            Assert.Null(scheduler.Find(selfKey));
        }

        [Fact]
        public void Tick_ThrowingParticipant_DoesNotStopHealthyParticipantsAndRecordsCounts()
        {
            var logSink = new CapturingLogSink();
            var scheduler = new AgentLoopScheduler(logSink);
            var firstHealthy = new StubAgentControl();
            var throwing = new StubAgentControl(onTick: () => throw new InvalidOperationException("boom"));
            var secondHealthy = new StubAgentControl();
            scheduler.Register(AgentLoopKeys.ForPawn(18), AgentLoopKind.Pawn, firstHealthy);
            scheduler.Register(AgentLoopKeys.ForScoped("bad"), AgentLoopKind.Scoped, throwing);
            scheduler.Register(AgentLoopKeys.ForPawn(19), AgentLoopKind.Pawn, secondHealthy);

            scheduler.Tick(400);

            var snapshot = scheduler.GetSnapshot();
            Assert.Equal(1, firstHealthy.TickCount);
            Assert.Equal(1, throwing.TickCount);
            Assert.Equal(1, secondHealthy.TickCount);
            Assert.Equal(2, snapshot.TickedAgents);
            Assert.Equal(1, snapshot.FaultedAgents);
            Assert.Equal(
                "[RimMind.AgentLoop] action=TickFailed key=scope:bad kind=Scoped error=InvalidOperationException: boom",
                Assert.Single(logSink.Errors));
        }

        [Fact]
        public void GetSnapshot_CountsKindsAndStates()
        {
            var scheduler = new AgentLoopScheduler();
            scheduler.Register(AgentLoopKeys.ForPawn(20), AgentLoopKind.Pawn, new StubAgentControl(AgentState.Active));
            scheduler.Register(AgentLoopKeys.ForPawn(21), AgentLoopKind.Pawn, new StubAgentControl(AgentState.Paused));
            scheduler.Register(AgentLoopKeys.ForScoped("ended"), AgentLoopKind.Scoped, new StubAgentControl(AgentState.Terminated));
            scheduler.Register(AgentLoopKeys.ForScoped("waiting"), AgentLoopKind.Scoped, new StubAgentControl(AgentState.Dormant));

            scheduler.Tick(500);

            var snapshot = scheduler.GetSnapshot();
            Assert.Equal(2, snapshot.RegisteredPawnAgents);
            Assert.Equal(2, snapshot.RegisteredScopedAgents);
            Assert.Equal(1, snapshot.ActiveAgents);
            Assert.Equal(1, snapshot.PausedAgents);
            Assert.Equal(1, snapshot.PendingAgents);
            Assert.Equal(1, snapshot.TerminatedAgents);
            Assert.Equal(500, snapshot.LastTick);
            Assert.Equal(4, snapshot.TickedAgents);
            Assert.Equal(0, snapshot.FaultedAgents);
        }

        [Fact]
        public void Empty_HasZeroCountsAndNoLastTick()
        {
            var snapshot = AgentLoopSnapshot.Empty;

            Assert.Equal(0, snapshot.RegisteredPawnAgents);
            Assert.Equal(0, snapshot.RegisteredScopedAgents);
            Assert.Equal(0, snapshot.ActiveAgents);
            Assert.Equal(0, snapshot.PausedAgents);
            Assert.Equal(0, snapshot.PendingAgents);
            Assert.Equal(0, snapshot.TerminatedAgents);
            Assert.Equal(-1, snapshot.LastTick);
            Assert.Equal(0, snapshot.TickedAgents);
            Assert.Equal(0, snapshot.FaultedAgents);
        }

        [Fact]
        public void RegistryMutations_NeverCleanupOrDestroyAgents()
        {
            var scheduler = new AgentLoopScheduler();
            var original = new StubAgentControl();
            var replacement = new StubAgentControl();
            var retainedUntilClear = new StubAgentControl();
            var key = AgentLoopKeys.ForPawn(22);
            scheduler.Register(key, AgentLoopKind.Pawn, original);
            scheduler.Register(key, AgentLoopKind.Pawn, replacement);
            scheduler.Register(AgentLoopKeys.ForScoped("clear"), AgentLoopKind.Scoped, retainedUntilClear);

            scheduler.Unregister(key);
            scheduler.Clear();

            Assert.Equal(0, original.CleanupCount);
            Assert.Equal(0, original.DestroyCount);
            Assert.Equal(0, replacement.CleanupCount);
            Assert.Equal(0, replacement.DestroyCount);
            Assert.Equal(0, retainedUntilClear.CleanupCount);
            Assert.Equal(0, retainedUntilClear.DestroyCount);
            Assert.Equal(0, scheduler.GetSnapshot().RegisteredPawnAgents);
            Assert.Equal(0, scheduler.GetSnapshot().RegisteredScopedAgents);
        }

        private sealed class StubAgentControl : IAgentControl
        {
            private readonly Action? _onTick;

            public StubAgentControl(AgentState state = AgentState.Active, Action? onTick = null)
            {
                State = state;
                _onTick = onTick;
            }

            public AgentState State { get; private set; }
            public bool IsActive => State == AgentState.Active;
            public AgentModeId CurrentModeId => AgentModeId.Dormant;
            public IAgentMode CurrentMode => null!;
            public bool IsPawnValid => true;
            public string NpcId => "stub";
            public string Label => "Stub agent";
            public int? LastThinkTick { get; set; }
            public int GoalCount => 0;
            public int TickCount { get; private set; }
            public int CleanupCount { get; private set; }
            public int DestroyCount { get; private set; }

            public void Tick()
            {
                TickCount++;
                _onTick?.Invoke();
            }

            public bool TransitionTo(AgentState newState)
            {
                State = newState;
                return true;
            }

            public void ForceThink()
            {
            }

            public void SwitchMode(AgentModeId modeId)
            {
            }

            public void Cleanup()
            {
                CleanupCount++;
            }

            public void Destroy()
            {
                DestroyCount++;
            }

            public void ResubscribeEvents()
            {
            }

            public bool RemoveGoal(string goalDescription) => false;

            public void RecordBehavior(BehaviorRecordDto record)
            {
            }

            public IReadOnlyList<BehaviorRecordDto> GetRecentHistory(int count = 10) =>
                Array.Empty<BehaviorRecordDto>();

            public float GetRecentSuccessRate(int count = 10) => 0f;

            public object? ConsumePendingJob() => null;

            public string GetDebugInfo() => string.Empty;
        }

        private sealed class CapturingLogSink : ILogSink
        {
            public List<string> Errors { get; } = new();

            public void Message(string msg)
            {
            }

            public void Warning(string msg)
            {
            }

            public void Error(string msg)
            {
                Errors.Add(msg);
            }

            public void LogFromBackground(string msg, bool isWarning = false)
            {
            }
        }
    }
}
