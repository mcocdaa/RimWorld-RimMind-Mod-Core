using System.Reflection;
using RimMind.Domain.Events;

namespace RimMind.IntegrationTests.Events
{
    public class SocialEventProposedEventTests
    {
        [Fact]
        public void AccessedAsBase_ShouldExposeCorrectBusEventType()
        {
            var evt = new SocialEventProposedEvent(
                "npc1",
                1,
                "evt1",
                SocialEventType.Party,
                "npc1",
                "A colony gathering",
                12345,
                678);

            AgentBusEvent asBase = evt;

            asBase.BusEventType.Should().Be(AgentBusEventType.SocialEventProposed);
        }

        [Fact]
        public void DerivedEventType_ShouldRemainSocialEventType()
        {
            var evt = new SocialEventProposedEvent(
                "npc1",
                1,
                "evt1",
                SocialEventType.Party,
                "npc1",
                "A colony gathering",
                12345,
                678);

            evt.EventType.Should().Be(SocialEventType.Party);
        }

        [Fact]
        public void DerivedClass_ShouldNotDeclareBusEventTypeField()
        {
            var derivedFields = typeof(SocialEventProposedEvent)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            derivedFields.Should().NotContain(field => field.Name == "BusEventType");
        }
    }
}
