using RimMind.Domain.Events;

namespace RimMind.IntegrationTests.Events
{
    public class EventTimestampTests
    {
        [Fact]
        public void PerceptionEvent_ShouldInitializeBaseFields()
        {
            var evt = new PerceptionEvent("npc-1", 101, "sight", "Saw a threat", 0.75f, timestamp: 12345);

            AssertBaseFields(evt, "npc-1", 101, AgentBusEventType.Perception, 12345);
        }

        [Fact]
        public void ActionEvent_ShouldInitializeBaseFields()
        {
            var evt = new ActionEvent("npc-2", 202, "EquipWeapon", true, "Equipped rifle", "event-2", timestamp: 23456);

            AssertBaseFields(evt, "npc-2", 202, AgentBusEventType.Action, 23456);
        }

        [Fact]
        public void DecisionEvent_ShouldInitializeBaseFields()
        {
            var evt = new DecisionEvent("npc-3", 303, "Combat", "Enemy nearby", "TakeCover", timestamp: 34567);

            AssertBaseFields(evt, "npc-3", 303, AgentBusEventType.Decision, 34567);
        }

        [Fact]
        public void GoalEvent_ShouldInitializeBaseFields()
        {
            var evt = new GoalEvent("npc-4", 404, "Secure food", "Active", "Survival", timestamp: 45678);

            AssertBaseFields(evt, "npc-4", 404, AgentBusEventType.Goal, 45678);
        }

        [Fact]
        public void AgentLifecycleEvent_ShouldInitializeBaseFields()
        {
            var evt = new AgentLifecycleEvent("npc-5", 505, "Idle", "Active", timestamp: 56789);

            AssertBaseFields(evt, "npc-5", 505, AgentBusEventType.Lifecycle, 56789);
        }

        private static void AssertBaseFields(
            AgentBusEvent evt,
            string expectedNpcId,
            int expectedPawnId,
            AgentBusEventType expectedBusEventType,
            int expectedTimestamp)
        {
            evt.NpcId.Should().Be(expectedNpcId);
            evt.PawnId.Should().Be(expectedPawnId);
            evt.BusEventType.Should().Be(expectedBusEventType);
            evt.Timestamp.Should().Be(expectedTimestamp);
        }
    }
}
