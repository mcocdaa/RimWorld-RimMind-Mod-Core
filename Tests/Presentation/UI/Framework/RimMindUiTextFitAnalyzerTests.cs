using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class RimMindUiTextFitAnalyzerTests
{
    [Fact]
    public void Analyze_ButtonTextTooLong_ReturnsWarning()
    {
        var document = new RimMindUiDocument("bad_button", new Rect(0f, 0f, 300f, 100f), new[]
        {
            RimMindUiElement.Button("button", new Rect(0f, 0f, 60f, 30f), "Very Long Button Label")
        });

        var warnings = RimMindUiTextFitAnalyzer.Analyze(document);

        Assert.Contains(warnings, warning => warning.ElementName == "button");
    }

    [Fact]
    public void Analyze_ReasonableButtonText_NoWarning()
    {
        var document = new RimMindUiDocument("ok_button", new Rect(0f, 0f, 300f, 100f), new[]
        {
            RimMindUiElement.Button("button", new Rect(0f, 0f, 160f, 30f), "Pause")
        });

        var warnings = RimMindUiTextFitAnalyzer.Analyze(document);

        Assert.Empty(warnings);
    }
}
