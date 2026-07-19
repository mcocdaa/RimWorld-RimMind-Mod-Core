using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseStructure
{
    public class R_S4_ColonyDirExistsTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        [Fact(Skip = "Colony/ 目录为 P1/P2 机制预留，尚未创建"), Trait("Phase", "Structure")]
        public void Mechanisms_Colony_Dir_Should_Exist()
        {
            var colonyDir = Path.Combine(SourceRoot, "Infrastructure", "Mechanisms", "Colony");
            Directory.Exists(colonyDir).Should().BeTrue(
                "Colony/ 目录为 P1/P2 机制预留");
        }

        [Fact, Trait("Phase", "Structure")]
        public void Application_AgentMode_Dir_Should_Exist()
        {
            var agentModeDir = Path.Combine(SourceRoot, "Application", "Features", "Agent", "Modes");
            Directory.Exists(agentModeDir).Should().BeTrue(
                "Agent/Modes/ directory must exist for I phase (IAgentMode + IThinkStrategy implementations)");
        }

        [Fact, Trait("Phase", "Structure")]
        public void Application_Pipeline_Unified_Dir_Should_Exist()
        {
            var unifiedDir = Path.Combine(SourceRoot, "Application", "Features", "Pipeline", "Unified");
            Directory.Exists(unifiedDir).Should().BeTrue(
                "Pipeline/Unified/ 目录为 K 阶段预留");
        }
    }
}
