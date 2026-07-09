using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI9_DebugCenterOptimizationCoverageTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI9_SnapshotArtifacts_ShouldExist_ForDebugCenterOptimizationStates()
    {
        string snapshotDir = ResolveSnapshotDirectory();
        var requiredSnapshots = new[]
        {
            "debug_overview",
            "agent_active",
            "agent_pending",
            "agent_paused",
            "agent_error",
            "requests_mixed_status",
            "toolcalls_mixed_status",
            "context_keys_dense"
        };

        var missing = new List<string>();
        foreach (string snapshot in requiredSnapshots)
        {
            foreach (string extension in new[] { ".html", ".svg" })
            {
                string fileName = snapshot + extension;
                if (!File.Exists(Path.Combine(snapshotDir, fileName)))
                    missing.Add(fileName);
            }
        }

        missing.Should().BeEmpty("CLEAN_UI_ERROR R-UI9: debug center optimization states must have html and svg snapshot artifacts.");
    }

    [Fact]
    public void R_UI9_DebugCenterPageRegistry_ShouldDefaultToOverview()
    {
        string registryPath = Path.Combine(
            SourceDir,
            "Infrastructure",
            "UI",
            "DebugCenter",
            "DebugCenterPageRegistry.cs");

        string text = File.ReadAllText(registryPath);
        text.Should().Contain(
            "Register(new DebugCenterPageDescriptor(\r\n                \"overview\",\r\n                \"RimMind.UI.Hub.Tab.Overview\",\r\n                0,\r\n                IsDefault: true)",
            "CLEAN_UI_ERROR R-UI9: DebugCenterPageRegistry must register overview as the default debug center page.");
    }

    private static string ResolveSnapshotDirectory()
    {
        string coreRoot = Directory.GetParent(SourceDir)?.FullName
            ?? throw new InvalidOperationException("Could not resolve RimMind-Core root from Source directory.");

        return Path.Combine(coreRoot, "Tests", "_snapshots", "ui");
    }
}
