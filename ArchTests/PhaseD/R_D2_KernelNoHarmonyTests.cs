using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseD
{
    public class ApplicationNoHarmonyTests
    {
        private static string GetApplicationCsprojPath()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            return Path.Combine(sourceDir, "Application", "RimMindCore.Application.csproj");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Application_ShouldNot_Reference_Harmony()
        {
            var csprojPath = GetApplicationCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Application csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeFalse(
                "R-D2: 1_RimMindApplication.dll must NOT reference 0Harmony (Lib.Harmony.Ref). " +
                "Application is the use-case layer — Harmony patching belongs exclusively in Infrastructure/Patches.");

            analysis.HasPackageRef("0Harmony").Should().BeFalse(
                "R-D2: 1_RimMindApplication.dll must NOT reference 0Harmony.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Application_AssemblyName_ShouldBe_Prefixed()
        {
            var csprojPath = GetApplicationCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Application csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.AssemblyName.Should().Be("1_RimMindApplication",
                "R-D2: Application assembly name must be '1_RimMindApplication' to ensure " +
                "it loads second (after Domain) in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Application_Should_Reference_Domain_And_Newtonsoft()
        {
            var csprojPath = GetApplicationCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Application csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasProjectRef("RimMindCore.Domain").Should().BeTrue(
                "R-D2: Application must reference Domain via ProjectReference. " +
                "Application uses domain types defined in Domain.");

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeTrue(
                "R-D2: Application must reference Newtonsoft.Json for JSON serialization. " +
                "Application handles serialization of context data and prompts.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Dll_Application_ShouldNot_Reference_Harmony()
        {
            if (!ArchTestExtensions.TryLocateAssembly("1_RimMindApplication.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().NotContain("0Harmony",
                "R-D2 (DLL): 1_RimMindApplication.dll must NOT reference 0Harmony. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Dll_Application_Should_Reference_Domain()
        {
            if (!ArchTestExtensions.TryLocateAssembly("1_RimMindApplication.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().Contain("0_RimMindDomain",
                "R-D2 (DLL): 1_RimMindApplication.dll must reference 0_RimMindDomain. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }
    }
}
