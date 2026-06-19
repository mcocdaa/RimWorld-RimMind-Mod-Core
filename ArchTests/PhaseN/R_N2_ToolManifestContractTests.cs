using System.IO;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N2_ToolManifestContractTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        private static readonly string ToolManifestPath = Path.Combine(
            SourceRoot,
            "Application",
            "Common",
            "Models",
            "Tools",
            "ToolManifest.cs");

        private static readonly string ToolRegistryInterfacePath = Path.Combine(
            SourceRoot,
            "Application",
            "Common",
            "Interfaces",
            "Tools",
            "IToolRegistry.cs");

        [Fact]
        public void ToolManifest_Model_Exists_In_Application_Tool_Models()
        {
            Assert.True(File.Exists(ToolManifestPath), "ToolManifest.cs must exist under Application/Common/Models/Tools.");
        }

        [Fact]
        public void ToolManifest_Model_Defines_Scope_Aware_Contract()
        {
            Assert.True(File.Exists(ToolManifestPath), "ToolManifest.cs must exist under Application/Common/Models/Tools.");

            var content = File.ReadAllText(ToolManifestPath);

            Assert.Contains("public sealed record ToolManifest", content);
            Assert.Contains("string OwnerModId", content);
            Assert.Contains("IReadOnlyList<AgentScopeKind> AllowedScopes", content);
            Assert.Contains("ToolRiskLevel RiskLevel", content);
            Assert.Contains("bool RequiresApproval", content);
            Assert.Contains("string SchemaVersion", content);
        }

        [Fact]
        public void ToolRegistry_Interface_Exposes_Scope_Aware_Query_Methods()
        {
            Assert.True(File.Exists(ToolRegistryInterfacePath), "IToolRegistry.cs must exist.");

            var content = File.ReadAllText(ToolRegistryInterfacePath);

            Assert.Contains("GetDefinitionsForScope(AgentScopeKind scopeKind)", content);
            Assert.Contains("GetHandlersForScope(AgentScopeKind scopeKind)", content);
        }
    }
}
