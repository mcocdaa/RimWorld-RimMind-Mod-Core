using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Features.Agent.Psychology;
using RimMind.Domain.Agent.Psychology;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Psychology
{
    public class DefaultPsychologyWatcherTests
    {
        private readonly StubTickProvider _tick = new() { TicksGame = 10000 };
        private readonly StubAgentBus _bus = new();
        private readonly StubPsychologyDataProvider _psychData = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };
        private const int PawnId = 42;

        private DefaultPsychologyWatcher CreateWatcher()
            => new(_tick, _bus, _psychData);

        [Fact]
        public void CheckAndPublish_ThrottlesByCheckInterval()
        {
            var watcher = CreateWatcher();
            // Use a mood that triggers MentalStateWarning to verify event count
            _psychData.MoodLevel = 0.08f;
            _psychData.MentalBreakThreshold = 0.1f;

            // First call at tick 10000
            watcher.CheckAndPublish(_agent, PawnId);
            var firstCount = _bus.PublishedEvents.Count;
            Assert.True(firstCount > 0, "First check should publish at least one event");

            // Second call at tick 11000 (< 1500 interval from 10000)
            _tick.TicksGame = 11000;
            watcher.CheckAndPublish(_agent, PawnId);

            // Second call should be throttled, no new events
            Assert.Equal(firstCount, _bus.PublishedEvents.Count);
        }

        [Fact]
        public void CheckAndPublish_MoodThresholdCrossed_PublishesEvent()
        {
            var watcher = CreateWatcher();
            _psychData.MoodLevel = 0.7f; // Normal
            _psychData.MentalBreakThreshold = 0.1f;

            // First check: establishes baseline
            watcher.CheckAndPublish(_agent, PawnId);
            _bus.PublishedEvents.Clear();

            // Advance tick past throttle
            _tick.TicksGame = 12000;

            // Mood drops from Normal (0.7) to Low (0.4)
            _psychData.MoodLevel = 0.4f;
            watcher.CheckAndPublish(_agent, PawnId);

            Assert.Contains(_bus.PublishedEvents, e => e is MoodThresholdCrossedEvent);
        }

        [Fact]
        public void CheckAndPublish_NeedCritical_SetsHasUrgentEvent()
        {
            var watcher = CreateWatcher();
            _psychData.MoodLevel = 0.8f;
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>
            {
                new() { NeedId = "food", CurrentLevel = 0.05f } // Critical: < 0.1
            };

            watcher.CheckAndPublish(_agent, PawnId);
            Assert.True(watcher.HasUrgentEvent("npc-1"));
        }

        [Fact]
        public void CheckAndPublish_NeedNotCritical_NoUrgentEvent()
        {
            var watcher = CreateWatcher();
            _psychData.MoodLevel = 0.8f;
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>
            {
                new() { NeedId = "food", CurrentLevel = 0.5f } // Low urgency
            };

            watcher.CheckAndPublish(_agent, PawnId);
            Assert.False(watcher.HasUrgentEvent("npc-1"));
        }

        [Fact]
        public void CheckAndPublish_MoodBelowBreakThreshold_PublishesImminentWarning()
        {
            var watcher = CreateWatcher();
            _psychData.MoodLevel = 0.08f;
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>();

            watcher.CheckAndPublish(_agent, PawnId);

            Assert.Contains(_bus.PublishedEvents, e =>
                e is MentalStateWarningEvent m && m.WarningLevel == "imminent");
            Assert.True(watcher.HasUrgentEvent("npc-1"));
        }

        [Fact]
        public void CheckAndPublish_MoodApproachingBreakThreshold_PublishesApproachingWarning()
        {
            var watcher = CreateWatcher();
            // breakThreshold = 0.1, approachThreshold = 0.1 * 1.2 = 0.12
            _psychData.MoodLevel = 0.11f; // Between approach and break
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>();

            watcher.CheckAndPublish(_agent, PawnId);

            Assert.Contains(_bus.PublishedEvents, e =>
                e is MentalStateWarningEvent m && m.WarningLevel == "approaching");
        }

        [Fact]
        public void CheckAndPublish_MoodAboveApproach_NoMentalWarning()
        {
            var watcher = CreateWatcher();
            _psychData.MoodLevel = 0.8f;
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>();

            watcher.CheckAndPublish(_agent, PawnId);

            Assert.DoesNotContain(_bus.PublishedEvents, e => e is MentalStateWarningEvent);
        }

        [Fact]
        public void HasUrgentEvent_UnknownNpc_ReturnsFalse()
        {
            var watcher = CreateWatcher();
            Assert.False(watcher.HasUrgentEvent("unknown-npc"));
        }

        [Fact]
        public void CheckAndPublish_AfterInterval_AllowsSecondCheck()
        {
            var watcher = CreateWatcher();
            // Use a mood that triggers MentalStateWarning to verify events
            _psychData.MoodLevel = 0.08f;
            _psychData.MentalBreakThreshold = 0.1f;
            _psychData.NeedLevels = new List<NeedLevel>();

            // First check
            watcher.CheckAndPublish(_agent, PawnId);
            var firstCount = _bus.PublishedEvents.Count;
            Assert.True(firstCount > 0, "First check should publish events");

            // Advance tick past throttle (1500)
            _tick.TicksGame = 12000;
            watcher.CheckAndPublish(_agent, PawnId);

            // Second check should add more events
            Assert.True(_bus.PublishedEvents.Count > firstCount);
        }

        [Fact]
        public void Constructor_NullTickProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultPsychologyWatcher(null!, _bus, _psychData));
        }

        [Fact]
        public void Constructor_NullAgentBus_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultPsychologyWatcher(_tick, null!, _psychData));
        }

        [Fact]
        public void Constructor_NullPsychologyDataProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultPsychologyWatcher(_tick, _bus, null!));
        }
    }
}
