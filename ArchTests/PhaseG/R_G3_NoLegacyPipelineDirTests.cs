using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseG
{
    public class R_G3_NoLegacyPipelineDirTests
    {
        [Fact]
        [Trait("Phase", "G")]
        public void R_G3_Presentation_Pipeline_Directory_Should_Not_Exist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist");

            var presentationPipelineDir = Path.Combine(sourceDir, "Presentation", "Pipeline");
            Directory.Exists(presentationPipelineDir).Should().BeFalse(
                "R-G3: Source/Presentation/Pipeline/ must not exist. Pipeline middleware belongs in Application/Pipeline/.");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(R_G3_NoLegacyPipelineDirTests).Assembly.Location);
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
