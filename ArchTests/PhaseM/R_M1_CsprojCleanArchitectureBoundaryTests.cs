using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M1_CsprojCleanArchitectureBoundaryTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    private static IReadOnlyList<string> FindInternalsVisibleToIncludes(string csprojText)
    {
        return Regex.Matches(csprojText, @"<InternalsVisibleTo\s+Include=""([^""]+)""\s*/>")
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Concat(Regex.Matches(csprojText, @"<_Parameter1>([^<]+)</_Parameter1>")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value))
            .ToList();
    }

    private static bool IsTestAssembly(string assemblyName)
        => assemblyName.StartsWith("RimMindCore.Tests", StringComparison.OrdinalIgnoreCase)
        || assemblyName.StartsWith("RimMindCore.ArchTests", StringComparison.OrdinalIgnoreCase)
        || assemblyName.StartsWith("RimMindCore.Integration.Tests", StringComparison.OrdinalIgnoreCase);

    [Fact]
    [Trait("Phase", "M")]
    public void R_M1_Domain_Csproj_Should_Have_No_Production_Friends_Or_External_Refs()
    {
        string path = Path.Combine(SourceDir, "Domain", "RimMindCore.Domain.csproj");
        var analysis = ArchTestExtensions.AnalyzeCsproj(path);
        string text = File.ReadAllText(path);
        var forbiddenFriends = FindInternalsVisibleToIncludes(text).Where(a => !IsTestAssembly(a)).ToList();

        analysis.ProjectReferences.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M1-CSPROJ: Domain must not reference any project. File: {0}",
            path);
        analysis.PackageReferences.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M1-CSPROJ: Domain must not reference any package. File: {0}",
            path);
        forbiddenFriends.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M1-CSPROJ: Domain internals must not be visible to production assemblies; promote required contracts to public types instead. Violations: {0}",
            string.Join(", ", forbiddenFriends));
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M1_Application_Csproj_Should_Not_Reference_RimWorld_Harmony_Or_Production_Friends()
    {
        string path = Path.Combine(SourceDir, "Application", "RimMindCore.Application.csproj");
        var analysis = ArchTestExtensions.AnalyzeCsproj(path);
        string text = File.ReadAllText(path);
        var forbiddenPackages = new[] { "Krafs.Rimworld.Ref", "Lib.Harmony.Ref", "0Harmony" }
            .Where(analysis.HasPackageRef)
            .ToList();
        var forbiddenFriends = FindInternalsVisibleToIncludes(text).Where(a => !IsTestAssembly(a)).ToList();

        analysis.HasProjectRef("RimMindCore.Domain").Should().BeTrue(
            "Application must depend inward on Domain");
        forbiddenPackages.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M1-CSPROJ: Application must not reference RimWorld/Harmony packages. Violations: {0}",
            string.Join(", ", forbiddenPackages));
        forbiddenFriends.Should().BeEmpty(
            "CLEAN_ARCH_ERROR R-M1-CSPROJ: Application internals must not be visible to production assemblies. Violations: {0}",
            string.Join(", ", forbiddenFriends));
    }

    [Fact]
    [Trait("Phase", "M")]
    public void R_M1_Core_Csproj_Should_Be_The_Only_RimWorld_And_Harmony_Adapter_Project()
    {
        string path = Path.Combine(SourceDir, "RimMindCore.csproj");
        var analysis = ArchTestExtensions.AnalyzeCsproj(path);

        analysis.HasProjectRef("RimMindCore.Application").Should().BeTrue();
        analysis.HasPackageRef("Krafs.Rimworld.Ref").Should().BeTrue();
        analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeTrue();
    }
}
