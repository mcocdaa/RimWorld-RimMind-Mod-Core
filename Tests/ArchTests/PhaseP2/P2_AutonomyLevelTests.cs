using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP2
{
    /// <summary>
    /// P2: AgentAutonomyLevel enum and IAgentAutonomySettings interface.
    /// Verifies that the autonomy level enum exists with four levels,
    /// the settings interface defines autonomy and risk-approval members,
    /// and IAgentTickSettings inherits from IAgentAutonomySettings.
    /// </summary>
    public class P2_AutonomyLevelTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath));

        [Fact]
        public void AgentAutonomyLevel_Enum_Exists_In_Domain_Enums()
        {
            var path = Path.Combine(SourceDir, "Domain", "Enums", "AgentAutonomyLevel.cs");
            Assert.True(File.Exists(path), "AgentAutonomyLevel.cs should exist in Domain/Enums");
            var content = ReadSourceFile("Domain/Enums/AgentAutonomyLevel.cs");
            Assert.Contains("AgentAutonomyLevel", content);
        }

        [Fact]
        public void AgentAutonomyLevel_Has_Four_Levels()
        {
            var content = ReadSourceFile("Domain/Enums/AgentAutonomyLevel.cs");
            Assert.Contains("Manual", content);
            Assert.Contains("Guided", content);
            Assert.Contains("Autonomous", content);
            Assert.Contains("Full", content);
        }

        [Fact]
        public void IAgentAutonomySettings_Exists_In_Application_Interfaces()
        {
            var path = Path.Combine(SourceDir, "Application", "Common", "Interfaces", "Internal", "IAgentAutonomySettings.cs");
            Assert.True(File.Exists(path), "IAgentAutonomySettings.cs should exist");
            var content = ReadSourceFile("Application/Common/Interfaces/Internal/IAgentAutonomySettings.cs");
            Assert.Contains("IAgentAutonomySettings", content);
        }

        [Fact]
        public void IAgentAutonomySettings_Has_AutonomyLevel_Property()
        {
            var content = ReadSourceFile("Application/Common/Interfaces/Internal/IAgentAutonomySettings.cs");
            Assert.Contains("AgentAutonomyLevel", content);
            Assert.Contains("AutonomyLevel", content);
        }

        [Fact]
        public void IAgentAutonomySettings_Has_RiskApproval_Method()
        {
            var content = ReadSourceFile("Application/Common/Interfaces/Internal/IAgentAutonomySettings.cs");
            Assert.Contains("ShouldApproveAction", content);
            Assert.Contains("RiskLevel", content);
        }

        [Fact]
        public void IAgentTickSettings_Inherits_IAgentAutonomySettings()
        {
            var content = ReadSourceFile("Application/Common/Interfaces/Internal/IAgentTickSettings.cs");
            Assert.Contains("IAgentAutonomySettings", content);
        }

        [Fact]
        public void PawnAgent_ExecuteDecision_Checks_AutonomyLevel()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnAgent.cs");
            Assert.Contains("ShouldApproveAction", content);
        }

        [Fact]
        public void PawnAgent_ExecuteDecision_Has_RiskLevel_Assessment()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnAgent.cs");
            Assert.Contains("RiskLevel", content);
        }

        [Fact]
        public void PawnAgent_Has_AssessRiskLevel_Method()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnAgent.cs");
            Assert.Contains("AssessRiskLevel", content);
        }

        [Fact]
        public void PawnAgent_Has_AutonomyLevel_Property()
        {
            var content = ReadSourceFile("Presentation/Agent/PawnAgent.cs");
            Assert.Contains("AgentAutonomyLevel", content);
            Assert.Contains("AutonomyLevel", content);
        }
    }
}
