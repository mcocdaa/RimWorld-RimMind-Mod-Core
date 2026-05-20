using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseStructure
{
    public class R_S2_RimMindApiLocTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        [Fact, Trait("Phase", "Structure")]
        public void RimMindAPI_Should_Be_Thin_Facade()
        {
            var apiFile = Path.Combine(SourceRoot, "RimMindAPI.cs");
            File.Exists(apiFile).Should().BeTrue("RimMindAPI.cs must exist");
            var lines = File.ReadAllLines(apiFile)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"))
                .Count();
            lines.Should().BeLessOrEqualTo(120,
                "RimMindAPI 应为瘦 Facade，具体逻辑在子 Facade 中");
        }
    }
}
