using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN;

public sealed class R_N4_CompositionRootSizeTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    [Trait("Phase", "N")]
    public void R_N4_CompositionRoot_Should_Not_Exceed_260_Lines()
    {
        string path = Path.Combine(SourceDir, "Presentation", "Runtime", "RimMindCompositionRoot.cs");

        File.Exists(path).Should().BeTrue("RimMindCompositionRoot.cs must exist.");
        File.ReadAllLines(path).Length.Should().BeLessOrEqualTo(260,
            "RimMindCompositionRoot should stay as orchestration; subsystem registration belongs in focused composition helpers.");
    }

    [Fact]
    [Trait("Phase", "N")]
    public void R_N4_Runtime_Composition_Folder_Should_Contain_Focused_Composition_Units()
    {
        string path = Path.Combine(SourceDir, "Presentation", "Runtime", "Composition");

        Directory.Exists(path).Should().BeTrue("focused runtime composition units must live under Presentation/Runtime/Composition.");

        string[] expectedFiles =
        {
            "SettingsComposition.cs",
            "ContextComposition.cs",
            "AgentComposition.cs",
            "ToolMechanismComposition.cs",
            "ClientComposition.cs",
            "UiComposition.cs"
        };

        foreach (string file in expectedFiles)
        {
            File.Exists(Path.Combine(path, file)).Should().BeTrue("{0} must exist under Presentation/Runtime/Composition.", file);
        }
    }
}
