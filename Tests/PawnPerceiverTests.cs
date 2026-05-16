using System;
using RimMind.Domain.Events;
using RimMind.Presentation.Agent;
using RimMind.Application.Features.AgentBus;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class PawnPerceiverTests : IDisposable
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly PawnPerceiver _perceiver;
        private AgentState _state = AgentState.Active;

        public PawnPerceiverTests()
        {
            RimMindRuntime.Initialize();
            _pawn = new Pawn { thingIDNumber = 42, Dead = false };
            _pawn.jobs = new Pawn_JobTracker { jobQueue = new Verse.AI.JobQueue() };
            RimMindCoreMod.Settings = new AICoreSettings
            {
                Context = new ContextSettings(),
                maxTokens = 800,
                defaultTemperature = 0.7f,
                thinkCooldownTicks = 30000,
                agentTickInterval = 150,
                behaviorHistoryMax = 100,
            };
            _eventBus = new EventBusAdapter(new AgentBusImpl());
            _perceiver = new PawnPerceiver(_pawn, _eventBus, () => _state);
        }

        public void Dispose()
        {
            _perceiver.Cleanup();
            RimMindCoreMod.Settings = null;
        }

        [Fact]
        public void Constructor_SubscribesToEventBus()
        {
            Assert.NotNull(_perceiver.Buffer);
        }

        [Fact]
        public void Collect_WithNoPerceptions_ReturnsEmptyList()
        {
            var result = _perceiver.Collect();
            Assert.Empty(result);
        }

        [Fact]
        public void Collect_AfterPerceptionEvent_ReturnsEntries()
        {
            var evt = new PerceptionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "sight", "saw something", 0.5f);
            _eventBus.Publish(evt);

            var result = _perceiver.Collect();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Collect_IgnoresPerceptionsWhenNotActive()
        {
            _state = AgentState.Dormant;
            var evt = new PerceptionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "sight", "saw something", 0.5f);
            _eventBus.Publish(evt);

            var result = _perceiver.Collect();
            Assert.Empty(result);
        }

        [Fact]
        public void Collect_IgnoresPerceptionsForOtherPawns()
        {
            var evt = new PerceptionEvent("NPC-999", 999, "sight", "saw something", 0.5f);
            _eventBus.Publish(evt);

            var result = _perceiver.Collect();
            Assert.Empty(result);
        }

        [Fact]
        public void ClearPending_RemovesAllPendingPerceptions()
        {
            var evt = new PerceptionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "sight", "saw something", 0.5f);
            _eventBus.Publish(evt);

            _perceiver.Collect();
            _perceiver.ClearPending();

            var result = _perceiver.Collect();
            Assert.Empty(result);
        }

        [Fact]
        public void Cleanup_UnsubscribesFromEventBus()
        {
            _perceiver.Cleanup();

            var evt = new PerceptionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "sight", "after cleanup", 0.5f);
            _eventBus.Publish(evt);

            _perceiver.Resubscribe();
            var result = _perceiver.Collect();
            Assert.Empty(result);
        }

        [Fact]
        public void Resubscribe_ReceivesEventsAgain()
        {
            _perceiver.Cleanup();
            _perceiver.Resubscribe();

            var evt = new PerceptionEvent($"NPC-{_pawn.thingIDNumber}", _pawn.thingIDNumber, "sight", "resubscribed", 0.5f);
            _eventBus.Publish(evt);

            var result = _perceiver.Collect();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Buffer_IsAccessible()
        {
            Assert.NotNull(_perceiver.Buffer);
            Assert.Equal(0, _perceiver.Buffer.Count);
        }
    }
}
