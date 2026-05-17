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
        public void R_A2_PawnAgent_Constructor_ShouldInject_IAgentBus()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var pawnAgentFile = Directory.GetFiles(sourceDir, "PawnAgent.cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            pawnAgentFile.Should().NotBeNull("PawnAgent.cs must exist in the source tree");

            var source = File.ReadAllText(pawnAgentFile!);

            var constructorPattern = @"public\s+PawnAgent\s*\([^)]*IAgentBus[^)]*\)";
            Regex.IsMatch(source, constructorPattern).Should().BeTrue(
                "R-A2: PawnAgent constructor must accept IAgentBus parameter for dependency injection. " +
                "Direct dependency on RimMindAPI.GetBus() breaks the dependency inversion principle — " +
                "the agent should receive its bus through constructor injection, not reach for a global static.");
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
