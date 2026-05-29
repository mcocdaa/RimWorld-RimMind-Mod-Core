using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-O5: DecisionFailedEvent Enablement.
    /// Verifies that DecisionProcessor publishes DecisionFailedEvent
    /// when AI request fails or ParseDecision fails.
    /// </summary>
    public class P1_O5_DecisionFailedEventTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent");

        private static readonly string DomainEventsDir = Path.Combine(
            ProjectRoot, "Source", "Domain", "Events");

        private static readonly string DecisionProcessorPath = Path.Combine(AgentDir, "DecisionProcessor.cs");
        private static readonly string DecisionFailedEventPath = Path.Combine(DomainEventsDir, "DecisionFailedEvent.cs");

        [Fact]
        public void DecisionFailedEvent_Class_Exists()
        {
            Assert.True(File.Exists(DecisionFailedEventPath),
                "DecisionFailedEvent.cs should exist in Domain/Events");
        }

        [Fact]
        public void DecisionFailedEvent_Inherits_AgentBusEvent()
        {
            var source = File.ReadAllText(DecisionFailedEventPath);
            Assert.Contains("DecisionFailedEvent : AgentBusEvent", source);
        }

        [Fact]
        public void DecisionFailedEvent_Sets_EventType_To_DecisionFailed()
        {
            var source = File.ReadAllText(DecisionFailedEventPath);
            Assert.Contains("AgentBusEventType.DecisionFailed", source);
        }

        [Fact]
        public void DecisionProcessor_Publishes_DecisionFailedEvent_On_AIRequestFailed()
        {
            var source = File.ReadAllText(DecisionProcessorPath);
            Assert.Contains("DecisionFailedEvent", source);
            Assert.Contains("AIRequestFailed", source);
        }

        [Fact]
        public void DecisionProcessor_Publishes_DecisionFailedEvent_On_ParseFailed()
        {
            var source = File.ReadAllText(DecisionProcessorPath);
            Assert.Contains("ParseFailed", source);
        }

        [Fact]
        public void DecisionProcessor_Uses_AgentBus_To_Publish_DecisionFailedEvent()
        {
            var source = File.ReadAllText(DecisionProcessorPath);
            Assert.Contains("_agentBus.Publish(new DecisionFailedEvent", source);
        }
    }
}
