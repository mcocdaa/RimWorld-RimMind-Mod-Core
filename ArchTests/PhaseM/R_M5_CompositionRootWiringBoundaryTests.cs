using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M5_CompositionRootWiringBoundaryTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    [Trait("Phase", "M")]
    public void R_M5_RimWorld_Mod_Entrypoint_Should_Not_Register_Services_Or_Open_Windows_Directly()
    {
        string path = Path.Combine(SourceDir, "AICoreMod.cs");
        string text = File.ReadAllText(path);

        Regex.IsMatch(text, @"RimMindServiceLocator\.(Register|Get|TryGet)\s*<").Should().BeFalse(
            "CLEAN_ARCH_ERROR R-M5-COMPOSITION-ROOT: AICoreMod must delegate service wiring to RimMindCompositionRoot/RimMindRuntime. Violating file: {0}. Fix: pass settings to RimMindRuntime.Initialize and use runtime/service abstractions.",
            path);
        text.Should().NotContain("Find.WindowStack",
            "CLEAN_ARCH_ERROR R-M5-COMPOSITION-ROOT: AICoreMod must not open Verse windows directly. Violating file: {0}. Fix: call IWindowService through RimMindRuntime.",
            path);
        text.Should().NotContain("Dialog_MessageBox",
            "CLEAN_ARCH_ERROR R-M5-COMPOSITION-ROOT: AICoreMod must not create concrete Verse dialogs directly. Violating file: {0}. Fix: add a window-service operation.",
            path);
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M5_DependencyInjection_Bags_Should_Not_Write_Global_ServiceLocator()
    {
        string[] files =
        {
            Path.Combine(SourceDir, "Application", "DependencyInjection.cs"),
            Path.Combine(SourceDir, "Infrastructure", "DependencyInjection.cs")
        };

        foreach (string path in files)
        {
            string text = File.ReadAllText(path);
            text.Should().NotContain("RimMindServiceLocator.Register",
                "CLEAN_ARCH_ERROR R-M5-COMPOSITION-ROOT: DependencyInjection helpers must only create service bags; CompositionRoot owns global registration. Violating file: {0}. Fix: return the service in a bag and register it from RimMindCompositionRoot.",
                path);
        }
    }
}
