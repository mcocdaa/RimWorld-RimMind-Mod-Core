using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseA
{
    public class PawnAgentConstructorTests
    {
        [Fact]
        [Trait("Phase", "A")]
        public void R_A2_PawnAgent_Constructor_ShouldAccept_Pawn_Only()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var pawnAgentFile = Directory.GetFiles(sourceDir, "PawnAgent.cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            pawnAgentFile.Should().NotBeNull("PawnAgent.cs must exist in the source tree");

            var source = File.ReadAllText(pawnAgentFile!);

            var pawnOnlyPattern = @"public\s+PawnAgent\s*\(\s*Pawn\s+pawn\s*\)";
            Regex.IsMatch(source, pawnOnlyPattern).Should().BeTrue(
                "R-A2: PawnAgent constructor must accept only Pawn parameter. " +
                "IAgentBus dependency was removed with Agent Modes cleanup — " +
                "the agent no longer needs bus injection in its constructor.");

            var busPattern = @"public\s+PawnAgent\s*\([^)]*IAgentBus[^)]*\)";
            Regex.IsMatch(source, busPattern).Should().BeFalse(
                "R-A2: PawnAgent must NOT have a constructor accepting IAgentBus. " +
                "Bus injection was a legacy pattern from Agent Modes that has been removed.");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(PawnAgentConstructorTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "RimMind-Core", "Source");
                if (Directory.Exists(candidate)) return candidate;

                candidate = Path.Combine(dir, "Source");
                if (Directory.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return "";
        }
    }
}
