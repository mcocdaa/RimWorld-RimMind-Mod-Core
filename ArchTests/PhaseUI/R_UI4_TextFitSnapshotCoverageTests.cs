using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI4_TextFitSnapshotCoverageTests
{
    [Fact]
    public void R_UI4_SnapshotTests_ShouldExist_ForMajorPages()
    {
        string sourceDir = ArchTestExtensions.FindSourceDirectory();
        string coreRoot = Directory.GetParent(sourceDir)?.FullName ?? string.Empty;
        string testDir = Path.Combine(coreRoot, "Tests", "Presentation", "UI", "Snapshots");
        var expected = new[]
        {
            "SettingsPageSnapshotTests.cs",
            "AgentPageSnapshotTests.cs",
            "DebugTableSnapshotTests.cs"
        };

        foreach (string file in expected)
            File.Exists(Path.Combine(testDir, file)).Should().BeTrue($"missing UI snapshot coverage file {file}");
    }
}
