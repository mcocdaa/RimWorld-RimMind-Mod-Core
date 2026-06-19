using System;
using System.IO;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N1_AgentScopeContractTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        private static readonly string AgentScopePath = Path.Combine(
            SourceRoot,
            "Application",
            "Common",
            "Models",
            "Agent",
            "AgentScope.cs");

        private static readonly string ManagerInterfacePath = Path.Combine(
            SourceRoot,
            "Application",
            "Common",
            "Interfaces",
            "Agent",
            "IScopedAgentManager.cs");

        private static readonly string ScopedAgentManagerPath = Path.Combine(
            SourceRoot,
            "Presentation",
            "Agent",
            "ScopedAgentManager.cs");

        [Fact]
        public void AgentScope_Model_Exists_In_Application_Agent_Models()
        {
            Assert.True(File.Exists(AgentScopePath), "AgentScope.cs must exist under Application/Common/Models/Agent.");
        }

        [Fact]
        public void AgentScope_Model_Defines_Typed_Contract()
        {
            Assert.True(File.Exists(AgentScopePath), "AgentScope.cs must exist under Application/Common/Models/Agent.");

            var content = File.ReadAllText(AgentScopePath);

            Assert.Contains("public sealed record AgentScope", content);
            Assert.Contains("AgentScopeKind Kind", content);
            Assert.Contains("string Id", content);
            Assert.Contains("public string CompositeKey", content);
            Assert.Contains("static AgentScope Pawn", content);
            Assert.Contains("static AgentScope Storyteller", content);
            Assert.Contains("static AgentScope Map", content);
            Assert.Contains("static AgentScope Thing", content);
        }

        [Fact]
        public void ScopedAgentManager_Interface_Exposes_Typed_Scope_Overloads()
        {
            Assert.True(File.Exists(ManagerInterfacePath), "IScopedAgentManager.cs must exist.");

            var content = File.ReadAllText(ManagerInterfacePath);

            Assert.Contains("GetOrCreate(AgentScope scope, IAgentBus agentBus)", content);
            Assert.Contains("Find(AgentScope scope)", content);
            Assert.Contains("Remove(AgentScope scope)", content);
        }

        [Fact]
        public void ScopedAgentManager_Keys_By_AgentScope_CompositeKey()
        {
            Assert.True(File.Exists(ScopedAgentManagerPath), "ScopedAgentManager.cs must exist.");

            var content = File.ReadAllText(ScopedAgentManagerPath);

            Assert.Contains("scope.CompositeKey", content);
            Assert.DoesNotContain("$\"{scopeType}:{scopeId}\"", content, StringComparison.Ordinal);
        }
    }
}
