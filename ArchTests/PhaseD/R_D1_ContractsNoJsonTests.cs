using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseD
{
    public class ContractsNoJsonTests
    {
        private static string GetContractsCsprojPath()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            return Path.Combine(sourceDir, "Contracts", "RimMindCore.Contracts.csproj");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Contracts_ShouldNot_Reference_Newtonsoft_Or_Harmony()
        {
            var csprojPath = GetContractsCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Contracts csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeFalse(
                "R-D1: 0_RimMindContracts.dll must NOT reference Newtonsoft.Json. " +
                "Contracts is the purest layer — only interfaces, enums, and DTOs. " +
                "JSON serialization belongs in Kernel or Adapters.");

            analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeFalse(
                "R-D1: 0_RimMindContracts.dll must NOT reference 0Harmony (Lib.Harmony.Ref). " +
                "Contracts must be Harmony-free. Patching logic belongs in Adapters/Patches.");

            analysis.HasPackageRef("0Harmony").Should().BeFalse(
                "R-D1: 0_RimMindContracts.dll must NOT reference 0Harmony.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Contracts_AssemblyName_ShouldBe_Prefixed()
        {
            var csprojPath = GetContractsCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Contracts csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.AssemblyName.Should().Be("0_RimMindContracts",
                "R-D1: Contracts assembly name must be '0_RimMindContracts' to ensure " +
                "it loads first in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Contracts_ShouldOnly_Reference_KrafsRimworldRef()
        {
            var csprojPath = GetContractsCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Contracts csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            var allowedPackageRefs = new[] { "Krafs.Rimworld.Ref" };
            var forbiddenPackageRefs = analysis.PackageReferences
                .Select(pr => pr.Include)
                .Where(name => !allowedPackageRefs.Contains(name))
                .ToList();

            forbiddenPackageRefs.Should().BeEmpty(
                "R-D1: Contracts may only reference Krafs.Rimworld.Ref (for Pawn etc.). " +
                "Forbidden package references found: " + string.Join(", ", forbiddenPackageRefs));
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Contracts_ShouldNot_Have_ProjectReferences()
        {
            var csprojPath = GetContractsCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Contracts csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.ProjectReferences.Should().BeEmpty(
                "R-D1: Contracts is the bottom layer — it must not have any ProjectReference. " +
                "Other layers reference Contracts, not the other way around.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Dll_Contracts_ShouldNot_Reference_Newtonsoft_Or_Harmony()
        {
            if (!ArchTestExtensions.TryLocateAssembly("0_RimMindContracts.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().NotContain("Newtonsoft.Json",
                "R-D1 (DLL): 0_RimMindContracts.dll must NOT reference Newtonsoft.Json. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");

            refs.Should().NotContain("0Harmony",
                "R-D1 (DLL): 0_RimMindContracts.dll must NOT reference 0Harmony. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }
    }
}
