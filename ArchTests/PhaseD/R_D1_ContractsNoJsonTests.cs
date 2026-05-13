using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseD
{
    public class DomainNoJsonTests
    {
        private static string GetDomainCsprojPath()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            return Path.Combine(sourceDir, "Domain", "RimMindCore.Domain.csproj");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Domain_ShouldNot_Reference_Newtonsoft_Or_Harmony()
        {
            var csprojPath = GetDomainCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Domain csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeFalse(
                "R-D1: 0_RimMindDomain.dll must NOT reference Newtonsoft.Json. " +
                "Domain is the purest layer — only value objects, enums, events, and exceptions. " +
                "JSON serialization belongs in Application or Infrastructure.");

            analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeFalse(
                "R-D1: 0_RimMindDomain.dll must NOT reference 0Harmony (Lib.Harmony.Ref). " +
                "Domain must be Harmony-free. Patching logic belongs in Infrastructure/Patches.");

            analysis.HasPackageRef("0Harmony").Should().BeFalse(
                "R-D1: 0_RimMindDomain.dll must NOT reference 0Harmony.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Domain_AssemblyName_ShouldBe_Prefixed()
        {
            var csprojPath = GetDomainCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Domain csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.AssemblyName.Should().Be("0_RimMindDomain",
                "R-D1: Domain assembly name must be '0_RimMindDomain' to ensure " +
                "it loads first in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Csproj_Domain_ShouldNot_Have_ProjectReferences_Or_PackageReferences()
        {
            var csprojPath = GetDomainCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Domain csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.ProjectReferences.Should().BeEmpty(
                "R-D1: Domain is the bottom layer — it must not have any ProjectReference. " +
                "Other layers reference Domain, not the other way around.");

            analysis.PackageReferences.Should().BeEmpty(
                "R-D1: Domain must have zero package dependencies. " +
                "It contains only pure domain types with no external dependencies.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D1_Dll_Domain_ShouldNot_Reference_Newtonsoft_Or_Harmony()
        {
            if (!ArchTestExtensions.TryLocateAssembly("0_RimMindDomain.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().NotContain("Newtonsoft.Json",
                "R-D1 (DLL): 0_RimMindDomain.dll must NOT reference Newtonsoft.Json. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");

            refs.Should().NotContain("0Harmony",
                "R-D1 (DLL): 0_RimMindDomain.dll must NOT reference 0Harmony. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }
    }
}
