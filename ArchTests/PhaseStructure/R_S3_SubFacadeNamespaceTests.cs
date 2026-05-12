using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseStructure
{
    public class R_S3_SubFacadeNamespaceTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        [Fact, Trait("Phase", "Structure")]
        public void RimMindAPI_SubFacades_Should_Exist_In_Api_Dir()
        {
            var apiDir = Path.Combine(SourceRoot, "Core", "Api");
            Directory.Exists(apiDir).Should().BeTrue("Core/Api/ directory must exist");
            var files = Directory.GetFiles(apiDir, "RimMindAPI.*.cs");
            files.Length.Should().BeGreaterOrEqualTo(9,
                "At least 9 sub-facade files should exist in Core/Api/");
        }

        [Fact, Trait("Phase", "Structure")]
        public void RimMindAPI_SubFacades_Should_Be_Partial_Class()
        {
            var apiDir = Path.Combine(SourceRoot, "Core", "Api");
            var files = Directory.GetFiles(apiDir, "RimMindAPI.*.cs");
            foreach (var f in files)
            {
                var content = File.ReadAllText(f);
                content.Should().Contain("static partial class RimMindAPI",
                    $"Sub-facade {Path.GetFileName(f)} should be a partial class of RimMindAPI");
            }
        }
    }
}
