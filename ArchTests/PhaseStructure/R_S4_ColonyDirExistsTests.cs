using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseStructure
{
    public class R_S4_ColonyDirExistsTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        [Fact, Trait("Phase", "Structure")]
        public void Mechanisms_Colony_Dir_Should_Exist()
        {
            var colonyDir = Path.Combine(SourceRoot, "Application", "Mechanisms", "Colony");
            Directory.Exists(colonyDir).Should().BeTrue(
                "Colony/ 目录为 P1/P2 机制预留");
        }

        [Fact, Trait("Phase", "Structure")]
        public void Application_AgentMode_Dir_Should_Exist()
        {
            var agentModeDir = Path.Combine(SourceRoot, "Application", "AgentMode");
            Directory.Exists(agentModeDir).Should().BeTrue(
                "AgentMode/ 目录为 I 阶段预留");
        }

        [Fact, Trait("Phase", "Structure")]
        public void Application_Pipeline_Unified_Dir_Should_Exist()
        {
            var unifiedDir = Path.Combine(SourceRoot, "Application", "Pipeline", "Unified");
            Directory.Exists(unifiedDir).Should().BeTrue(
                "Pipeline/Unified/ 目录为 K 阶段预留");
        }
    }
}
