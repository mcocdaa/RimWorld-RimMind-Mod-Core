using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class UiSnapshotWriterTests
{
    [Fact]
    public void HtmlWriter_EmitsNamedRectsAndLabels()
    {
        var document = new RimMindUiDocument(
            "debug_center_agent_active",
            new Rect(0f, 0f, 780f, 580f),
            new[]
            {
                RimMindUiElement.Panel("content", new Rect(10f, 10f, 300f, 200f)),
                RimMindUiElement.Button("pause", new Rect(20f, 20f, 120f, 30f), "Pause")
            });

        string html = UiSnapshotHtmlWriter.Write(document);

        Assert.Contains("debug_center_agent_active", html);
        Assert.Contains("data-name=\"pause\"", html);
        Assert.Contains("Pause", html);
    }

    [Fact]
    public void SvgWriter_EmitsFixedSizeSvg()
    {
        var document = new RimMindUiDocument(
            "settings_api",
            new Rect(0f, 0f, 780f, 580f),
            new[] { RimMindUiElement.Label("title", new Rect(12f, 12f, 180f, 30f), "RimMind") });

        string svg = UiSnapshotSvgWriter.Write(document);

        Assert.Contains("<svg", svg);
        Assert.Contains("width=\"780\"", svg);
        Assert.Contains("RimMind", svg);
    }

    [Fact]
    public void TableRowFactory_DefaultsSelectedToFalse()
    {
        var row = RimMindUiElement.TableRow("request", new Rect(1f, 2f, 120f, 24f), "Pending");

        Assert.Equal(RimMindUiElementKind.TableRow, row.Kind);
        Assert.Equal("request", row.Name);
        Assert.Equal("Pending", row.Text);
        Assert.False(row.Selected);
    }

    [Fact]
    public void TableHeaderFactory_DefaultsSelectedToFalse()
    {
        var header = RimMindUiElement.TableHeader("request_header", new Rect(1f, 2f, 120f, 24f), "Status");

        Assert.Equal(RimMindUiElementKind.TableHeader, header.Kind);
        Assert.Equal("request_header", header.Name);
        Assert.Equal("Status", header.Text);
        Assert.False(header.Selected);
    }

    [Fact]
    public void StatusStripFactory_DefaultsSelectedToFalse()
    {
        var strip = RimMindUiElement.StatusStrip("request_status", new Rect(1f, 2f, 120f, 24f), "Idle");

        Assert.Equal(RimMindUiElementKind.StatusStrip, strip.Kind);
        Assert.Equal("request_status", strip.Name);
        Assert.Equal("Idle", strip.Text);
        Assert.False(strip.Selected);
    }

    [Fact]
    public void Writers_EscapeTextAndNames()
    {
        var document = new RimMindUiDocument(
            "settings_<api>",
            new Rect(0f, 0f, 100f, 80f),
            new[] { RimMindUiElement.Button("save_\"key\"", new Rect(1f, 2f, 30f, 20f), "<Save & Close>") });

        string html = UiSnapshotHtmlWriter.Write(document);
        string svg = UiSnapshotSvgWriter.Write(document);

        Assert.Contains("settings_&lt;api&gt;", html);
        Assert.Contains("data-name=\"save_&quot;key&quot;\"", html);
        Assert.Contains("&lt;Save &amp; Close&gt;", html);
        Assert.Contains("settings_&lt;api&gt;", svg);
        Assert.Contains("&lt;Save &amp; Close&gt;", svg);
    }
}
