using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M4_CleanArchitectureDeployGateTests
{
    [Fact]
    [Trait("Phase", "M")]
    public void R_M4_Deploy_Should_Run_Clean_Architecture_Gate_By_Default()
    {
        string repoRoot = Directory.GetParent(Directory.GetParent(ArchTestExtensions.FindSourceDirectory())!.FullName)!.FullName;
        string deployPath = Path.Combine(repoRoot, "script", "deploy.ps1");
        string text = File.ReadAllText(deployPath);

        text.Should().Contain("[switch]$SkipCleanArchitectureVerification",
            "CLEAN_ARCH_ERROR R-M4-WARNINGS: deploy.ps1 must expose an explicit skip switch, not an opt-in verification switch.");
        text.Should().NotContain("[switch]$VerifyCleanArchitecture",
            "CLEAN_ARCH_ERROR R-M4-WARNINGS: clean architecture verification must be default-on before deploy.");
        text.Should().Contain("if (-not $SkipCleanArchitectureVerification)",
            "CLEAN_ARCH_ERROR R-M4-WARNINGS: deploy.ps1 must run verify-clean-architecture.ps1 unless explicitly skipped.");
        text.Should().Contain("WARNING: Clean architecture verification skipped by explicit user request.",
            "CLEAN_ARCH_ERROR R-M4-WARNINGS: skipping the clean architecture gate must be visible in deploy output.");
    }
}
