using System;
using System.IO;
using RimMind.Presentation.UI.Framework;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class UiSnapshotGenerationTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void GenerateStandaloneSnapshots()
    {
        string outputDir = Path.Combine(ProjectRoot, "Tests", "_snapshots", "ui");
        Directory.CreateDirectory(outputDir);

        foreach (RimMindUiDocument document in UiSnapshotCases.All())
        {
            string htmlPath = Path.Combine(outputDir, document.Id + ".html");
            string svgPath = Path.Combine(outputDir, document.Id + ".svg");
            File.WriteAllText(htmlPath, UiSnapshotHtmlWriter.Write(document));
            File.WriteAllText(svgPath, UiSnapshotSvgWriter.Write(document));
            Assert.True(File.Exists(htmlPath));
            Assert.True(File.Exists(svgPath));
        }
    }

    [Fact]
    public void AllStandaloneSnapshots_HaveNoTextFitWarnings()
    {
        foreach (RimMindUiDocument document in UiSnapshotCases.All())
        {
            var warnings = RimMindUiTextFitAnalyzer.Analyze(document);
            Assert.Empty(warnings);
        }
    }
}
