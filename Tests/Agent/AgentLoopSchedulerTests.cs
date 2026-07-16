using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task Tick_ConcurrentDifferentTick_SerializesParticipantExecution()
        {
            using var firstEntered = new ManualResetEventSlim();
            using var releaseFirst = new ManualResetEventSlim();
            var activeTicks = 0;
            var maximumActiveTicks = 0;
            var invocations = 0;
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl(onTick: () =>
            {
                var active = Interlocked.Increment(ref activeTicks);
                RecordMaximum(ref maximumActiveTicks, active);
                var invocation = Interlocked.Increment(ref invocations);
                try
                {
                    if (invocation == 1)
                    {
                        firstEntered.Set();
                        releaseFirst.Wait(TimeSpan.FromSeconds(10));
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeTicks);
                }
            });
            scheduler.Register(AgentLoopKeys.ForPawn(120), AgentLoopKind.Pawn, agent);

            var firstTask = Task.Run(() => scheduler.Tick(100));
            var enteredInTime = firstEntered.Wait(TimeSpan.FromSeconds(5));
            var secondTask = enteredInTime
                ? Task.Run(() => scheduler.Tick(101))
                : Task.CompletedTask;
            var secondReturnedInTime = enteredInTime
                && await Task.WhenAny(secondTask, Task.Delay(TimeSpan.FromSeconds(5))) == secondTask;
            releaseFirst.Set();
            var allTasks = Task.WhenAll(firstTask, secondTask);
            var allCompletedInTime = await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(5))) == allTasks;
            if (allCompletedInTime)
                await allTasks;

            Assert.True(enteredInTime);
            Assert.True(secondReturnedInTime);
            Assert.True(allCompletedInTime);
            Assert.Equal(1, maximumActiveTicks);
            Assert.Equal(2, invocations);
            Assert.Equal(101, scheduler.GetSnapshot().LastTick);
        }

        [Fact]
        public void Tick_RecursiveDifferentTick_QueuesLatestWithoutReentryAndKeepsLatestMetrics()
        {
            var activeTicks = 0;
            var maximumActiveTicks = 0;
            var invocations = 0;
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl(onTick: () =>
            {
                var active = Interlocked.Increment(ref activeTicks);
                RecordMaximum(ref maximumActiveTicks, active);
                var invocation = Interlocked.Increment(ref invocations);
                try
                {
                    if (invocation == 1)
                    {
                        scheduler.Tick(100);
                        scheduler.Tick(101);
                        scheduler.Tick(99);
                        scheduler.Tick(101);
                    }
                    else if (invocation == 2)
                    {
                        throw new InvalidOperationException("latest tick failed");
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeTicks);
                }
            });
            scheduler.Register(AgentLoopKeys.ForPawn(121), AgentLoopKind.Pawn, agent);

            scheduler.Tick(100);

            var snapshot = scheduler.GetSnapshot();
            Assert.Equal(1, maximumActiveTicks);
            Assert.Equal(2, invocations);
            Assert.Equal(101, snapshot.LastTick);
            Assert.Equal(0, snapshot.TickedAgents);
            Assert.Equal(1, snapshot.FaultedAgents);
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
        public void Register_InvalidKind_Throws()
        {
            var scheduler = new AgentLoopScheduler();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                scheduler.Register("pawn:invalid-kind", (AgentLoopKind)999, new StubAgentControl()));
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AgentLoopKeys_ForScopedRejectsNullOrBlankCompositeKey(string? compositeKey)
        {
            Assert.ThrowsAny<ArgumentException>(() => AgentLoopKeys.ForScoped(compositeKey!));
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
        public void GetSnapshot_StateGetterCanMutateRegistryWithoutInvalidationOrDeadlock()
        {
            var scheduler = new AgentLoopScheduler();
            var mutatingKey = AgentLoopKeys.ForPawn(122);
            var replacementKey = AgentLoopKeys.ForScoped("state-mutation");
            var replacement = new StubAgentControl();
            var hasMutated = false;
            var mutating = new StubAgentControl(onStateRead: () =>
            {
                if (hasMutated)
                    return;

                hasMutated = true;
                scheduler.Register(replacementKey, AgentLoopKind.Scoped, replacement);
                scheduler.Unregister(mutatingKey);
            });
            scheduler.Register(mutatingKey, AgentLoopKind.Pawn, mutating);
            scheduler.Register(AgentLoopKeys.ForPawn(123), AgentLoopKind.Pawn, new StubAgentControl());

            var snapshot = scheduler.GetSnapshot();

            Assert.True(hasMutated);
            Assert.Equal(2, snapshot.RegisteredPawnAgents);
            Assert.Equal(0, snapshot.RegisteredScopedAgents);
            Assert.Equal(2, snapshot.ActiveAgents);
            Assert.Null(scheduler.Find(mutatingKey));
            Assert.Same(replacement, scheduler.Find(replacementKey));
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

        [Fact]
        public void Clear_EmptiesRegistrationsAndIncrementsGenerationForEveryExplicitReset()
        {
            var scheduler = new AgentLoopScheduler();
            var initialGeneration = scheduler.Generation;
            scheduler.Register(
                AgentLoopKeys.ForPawn(23),
                AgentLoopKind.Pawn,
                new StubAgentControl());

            scheduler.Clear();

            Assert.Equal(initialGeneration + 1, scheduler.Generation);
            Assert.Equal(0, scheduler.GetSnapshot().RegisteredPawnAgents);

            scheduler.Clear();

            Assert.Equal(initialGeneration + 2, scheduler.Generation);
        }

        [Fact]
        public void RegisterAndUnregister_DoNotChangeGeneration()
        {
            var scheduler = new AgentLoopScheduler();
            IAgentLoopScheduler contract = scheduler;
            var initialGeneration = contract.Generation;
            var key = AgentLoopKeys.ForPawn(24);

            scheduler.Register(key, AgentLoopKind.Pawn, new StubAgentControl());
            Assert.Equal(initialGeneration, contract.Generation);

            scheduler.Unregister(key);
            Assert.Equal(initialGeneration, contract.Generation);
        }

        [Fact]
        public void Generation_ContractUsesLongEpoch()
        {
            var property = typeof(IAgentLoopScheduler).GetProperty("Generation");

            Assert.NotNull(property);
            Assert.Equal(typeof(long), property.PropertyType);
        }

        [Fact]
        public void Clear_AfterHighTick_ResetsMetricsAndAllowsTickZeroInNewEpoch()
        {
            var scheduler = new AgentLoopScheduler();
            var agent = new StubAgentControl();
            var key = AgentLoopKeys.ForPawn(25);
            scheduler.Register(key, AgentLoopKind.Pawn, agent);
            scheduler.Tick(5000);
            var generation = scheduler.Generation;

            scheduler.Clear();
            var resetSnapshot = scheduler.GetSnapshot();
            scheduler.Register(key, AgentLoopKind.Pawn, agent);
            scheduler.Tick(0);

            Assert.Equal(generation + 1, scheduler.Generation);
            Assert.Equal(-1, resetSnapshot.LastTick);
            Assert.Equal(0, resetSnapshot.TickedAgents);
            Assert.Equal(0, resetSnapshot.FaultedAgents);
            Assert.Equal(2, agent.TickCount);
            Assert.Equal(0, scheduler.GetSnapshot().LastTick);
        }

        [Fact]
        public void Clear_DuringActiveTick_PreventsOldEpochFromOverwritingResetMetrics()
        {
            var scheduler = new AgentLoopScheduler();
            var replacement = new StubAgentControl();
            var replacementKey = AgentLoopKeys.ForPawn(27);
            var resetting = new StubAgentControl(onTick: () =>
            {
                scheduler.Clear();
                scheduler.Register(replacementKey, AgentLoopKind.Pawn, replacement);
            });
            scheduler.Register(AgentLoopKeys.ForPawn(26), AgentLoopKind.Pawn, resetting);

            scheduler.Tick(5000);
            var resetSnapshot = scheduler.GetSnapshot();
            scheduler.Tick(0);

            Assert.Equal(-1, resetSnapshot.LastTick);
            Assert.Equal(0, resetSnapshot.TickedAgents);
            Assert.Equal(0, resetSnapshot.FaultedAgents);
            Assert.Equal(1, replacement.TickCount);
            Assert.Equal(0, scheduler.GetSnapshot().LastTick);
        }

        [Fact]
        public void Clear_DuringFirstParticipantTick_StopsRemainingOldEpochParticipants()
        {
            var scheduler = new AgentLoopScheduler();
            var staleParticipant = new StubAgentControl();
            var replacement = new StubAgentControl();
            var replacementKey = AgentLoopKeys.ForPawn(30);
            var resetting = new StubAgentControl(onTick: () =>
            {
                scheduler.Clear();
                scheduler.Register(replacementKey, AgentLoopKind.Pawn, replacement);
            });
            scheduler.Register(AgentLoopKeys.ForPawn(28), AgentLoopKind.Pawn, resetting);
            scheduler.Register(AgentLoopKeys.ForPawn(29), AgentLoopKind.Pawn, staleParticipant);

            scheduler.Tick(5000);
            var resetSnapshot = scheduler.GetSnapshot();
            scheduler.Tick(0);

            Assert.Equal(1, resetting.TickCount);
            Assert.Equal(0, staleParticipant.TickCount);
            Assert.Equal(-1, resetSnapshot.LastTick);
            Assert.Equal(0, resetSnapshot.TickedAgents);
            Assert.Equal(0, resetSnapshot.FaultedAgents);
            Assert.Equal(1, replacement.TickCount);
            Assert.Equal(0, scheduler.GetSnapshot().LastTick);
        }

        [Fact]
        public async Task Clear_DuringActiveTick_PreservesFirstPendingTickOfNewEpoch()
        {
            using var oldTickEntered = new ManualResetEventSlim();
            using var releaseOldTick = new ManualResetEventSlim();
            var scheduler = new AgentLoopScheduler();
            var oldAgent = new StubAgentControl(onTick: () =>
            {
                oldTickEntered.Set();
                releaseOldTick.Wait(TimeSpan.FromSeconds(10));
            });
            var newAgent = new StubAgentControl();
            scheduler.Register(AgentLoopKeys.ForPawn(31), AgentLoopKind.Pawn, oldAgent);

            Task oldTick = Task.Run(() => scheduler.Tick(100));
            Assert.True(oldTickEntered.Wait(TimeSpan.FromSeconds(5)));

            scheduler.Clear();
            scheduler.Register(AgentLoopKeys.ForPawn(32), AgentLoopKind.Pawn, newAgent);
            scheduler.Tick(101);
            releaseOldTick.Set();

            Task completed = await Task.WhenAny(oldTick, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(oldTick, completed);
            await oldTick;
            Assert.Equal(1, newAgent.TickCount);
            Assert.Equal(101, scheduler.GetSnapshot().LastTick);
        }

        private sealed class StubAgentControl : IAgentControl
        {
            private readonly Action? _onTick;
            private readonly Action? _onStateRead;
            private AgentState _state;

            public StubAgentControl(
                AgentState state = AgentState.Active,
                Action? onTick = null,
                Action? onStateRead = null)
            {
                _state = state;
                _onTick = onTick;
                _onStateRead = onStateRead;
            }

            public AgentState State
            {
                get
                {
                    _onStateRead?.Invoke();
                    return _state;
                }
            }
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
                _state = newState;
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

        private static void RecordMaximum(ref int maximum, int candidate)
        {
            var observed = Volatile.Read(ref maximum);
            while (candidate > observed)
            {
                var previous = Interlocked.CompareExchange(ref maximum, candidate, observed);
                if (previous == observed)
                    return;
                observed = previous;
            }
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
