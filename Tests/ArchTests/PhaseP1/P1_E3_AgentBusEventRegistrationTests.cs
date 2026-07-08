using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-E3: AgentBus Event Type Registration API.
    /// Verifies that IAgentBusAdministration has RegisterEventType method,
    /// AgentBusImpl implements it, and EventTypeMap includes all enum values.
    /// </summary>
    public class P1_E3_AgentBusEventRegistrationTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string InterfacesDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Common", "Interfaces");

        private static readonly string AgentBusImplPath = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "AgentBus", "AgentBusImpl.cs");

        private static readonly string IAgentBusAdminPath = Path.Combine(
            InterfacesDir, "IAgentBusAdministration.cs");

        [Fact]
        public void IAgentBusAdministration_Has_RegisterEventType_Method()
        {
            var source = File.ReadAllText(IAgentBusAdminPath);
            Assert.Contains("RegisterEventType", source);
        }

        [Fact]
        public void IAgentBusAdministration_RegisterEventType_Accepts_Name_And_Type()
        {
            var source = File.ReadAllText(IAgentBusAdminPath);
            Assert.Contains("string name", source);
            Assert.Contains("Type eventType", source);
        }

        [Fact]
        public void AgentBusImpl_Implements_RegisterEventType()
        {
            var source = File.ReadAllText(AgentBusImplPath);
            Assert.Contains("public void RegisterEventType(string name, Type eventType)", source);
        }

        [Fact]
        public void AgentBusImpl_RegisterEventType_Updates_EventTypeMap()
        {
            var source = File.ReadAllText(AgentBusImplPath);
            Assert.Contains("EventTypeMap.AddOrUpdate", source);
        }

        [Fact]
        public void AgentBusImpl_RegisterEventType_Validates_EventType_Base()
        {
            var source = File.ReadAllText(AgentBusImplPath);
            Assert.Contains("IsAssignableFrom", source);
        }

        [Fact]
        public void AgentBusImpl_EventTypeMap_Includes_DecisionFailed()
        {
            var source = File.ReadAllText(AgentBusImplPath);
            Assert.Contains("DecisionFailed", source);
        }

        [Fact]
        public void AgentBusImpl_EventTypeMap_Includes_WorkflowPhaseChange()
        {
            var source = File.ReadAllText(AgentBusImplPath);
            Assert.Contains("WorkflowPhaseChange", source);
        }
    }
}
