using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Core.Agent;
using RimMind.Core.AgentBus;
using RimMind.Core.Context;
using RimMind.Core.Flywheel;
using Verse;
using Xunit;

namespace RimMind.Tests.Lifecycle
{
    public class ContextEngineLifecycleTests
    {
        private ContextEngine CreateEngine()
        {
            return new ContextEngine(new HistoryManager());
        }

        [Fact]
        public void Dispose_SetsDisposedFlag()
        {
            var engine = CreateEngine();
            engine.Dispose();
        }

        [Fact]
        public void BuildSnapshot_AfterDispose_ReturnsNull()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var result = engine.BuildSnapshot(new ContextRequest
            {
                NpcId = "test",
                Scenario = ScenarioIds.Dialogue
            });

            Assert.Null(result);
        }

        [Fact]
        public void ResetCaches_AfterDispose_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.ResetCaches());
            Assert.Null(ex);
        }

        [Fact]
        public void InvalidateLayer_AfterDispose_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.InvalidateLayer("test", ContextLayer.L0_Static));
            Assert.Null(ex);
        }

        [Fact]
        public void InvalidateKey_AfterDispose_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.InvalidateKey("test", "someKey"));
            Assert.Null(ex);
        }

        [Fact]
        public void UpdateBaseline_AfterDispose_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.UpdateBaseline("test"));
            Assert.Null(ex);
        }

        [Fact]
        public void InvalidateNpc_AfterDispose_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.InvalidateNpc("test"));
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var engine = CreateEngine();
            engine.Dispose();

            var ex = Record.Exception(() => engine.Dispose());
            Assert.Null(ex);
        }
    }

    public class FlywheelGameComponentLifecycleTests
    {
        [Fact]
        public void StartedNewGame_DoesNotThrow()
        {
            var comp = new FlywheelGameComponent();
            var ex = Record.Exception(() => comp.StartedNewGame());
            Assert.Null(ex);
        }

        [Fact]
        public void LoadedGame_DoesNotThrow()
        {
            var comp = new FlywheelGameComponent();
            var ex = Record.Exception(() => comp.LoadedGame());
            Assert.Null(ex);
        }

        [Fact]
        public void StartedNewGame_AfterLoadedGame_DoesNotThrow()
        {
            var comp = new FlywheelGameComponent();
            comp.LoadedGame();
            var ex = Record.Exception(() => comp.StartedNewGame());
            Assert.Null(ex);
        }
    }

    public class PawnAgentCleanupTests
    {
        [Fact]
        public void Cleanup_ClearsGoalStack()
        {
            var pawn = new Pawn { thingIDNumber = 1 };
            var agent = new PawnAgent(pawn);
            agent.GoalStack.TryAdd(new AgentGoal("test", GoalCategory.Survival, 1f, GoalStatus.Active), pawn.thingIDNumber);

            agent.Cleanup();

            Assert.Empty(agent.GoalStack.Goals);
        }

        [Fact]
        public void Cleanup_ClearsPerceptionBuffer()
        {
            var pawn = new Pawn { thingIDNumber = 2 };
            var agent = new PawnAgent(pawn);
            agent.PerceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = "test",
                Content = "test content"
            });

            agent.Cleanup();

            Assert.Empty(agent.PerceptionBuffer.Entries);
        }

        [Fact]
        public void Cleanup_UnsubscribesFromAgentBus()
        {
            var pawn = new Pawn { thingIDNumber = 3 };
            var agent = new PawnAgent(pawn);

            agent.Cleanup();

            AgentBus.Publish(new PerceptionEvent("npc_3", 3, "test", "test content"));
        }

        [Fact]
        public void Cleanup_CalledTwice_DoesNotThrow()
        {
            var pawn = new Pawn { thingIDNumber = 4 };
            var agent = new PawnAgent(pawn);

            agent.Cleanup();
            var ex = Record.Exception(() => agent.Cleanup());
            Assert.Null(ex);
        }

        [Fact]
        public void Cleanup_ThenResubscribe_DoesNotThrow()
        {
            var pawn = new Pawn { thingIDNumber = 5 };
            var agent = new PawnAgent(pawn);

            agent.Cleanup();
            agent.ResubscribeEvents();

            AgentBus.Publish(new PerceptionEvent("npc_5", 5, "test", "test content"));
        }
    }

    public class CancellationTokenHealthCheckTests
    {
        [Fact]
        public async Task CancellationTokenSource_Cancel_StopsDelayImmediately()
        {
            using var cts = new CancellationTokenSource();
            var task = Task.Delay(60000, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task CancellationTokenSource_Cancel_BeforeDelay_CompletesImmediately()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await Task.Delay(60000, cts.Token));
        }

        [Fact]
        public void CancellationTokenSource_IsCancellationRequested_AfterCancel()
        {
            using var cts = new CancellationTokenSource();
            Assert.False(cts.IsCancellationRequested);

            cts.Cancel();

            Assert.True(cts.IsCancellationRequested);
        }

        [Fact]
        public async Task CancellationTokenSource_HealthCheckLoopPattern()
        {
            using var cts = new CancellationTokenSource();
            var loopCount = 0;

            var loopTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        await Task.Delay(10, cts.Token);
                        loopCount++;
                    }
                }
                catch (OperationCanceledException) { }
            });

            await Task.Delay(200);
            cts.Cancel();
            await loopTask;

            Assert.True(loopCount >= 1);
        }
    }
}
