using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseD
{
    public class KernelNoHarmonyTests
    {
        private static string GetKernelCsprojPath()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            return Path.Combine(sourceDir, "Kernel", "RimMindCore.Kernel.csproj");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Kernel_ShouldNot_Reference_Harmony()
        {
            var csprojPath = GetKernelCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Kernel csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeFalse(
                "R-D2: 1_RimMindKernel.dll must NOT reference 0Harmony (Lib.Harmony.Ref). " +
                "Kernel is the business logic layer — Harmony patching belongs exclusively in Adapters/Patches.");

            analysis.HasPackageRef("0Harmony").Should().BeFalse(
                "R-D2: 1_RimMindKernel.dll must NOT reference 0Harmony.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Kernel_AssemblyName_ShouldBe_Prefixed()
        {
            var csprojPath = GetKernelCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Kernel csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.AssemblyName.Should().Be("1_RimMindKernel",
                "R-D2: Kernel assembly name must be '1_RimMindKernel' to ensure " +
                "it loads second (after Contracts) in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Kernel_Should_Reference_Contracts_And_Newtonsoft()
        {
            var csprojPath = GetKernelCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Kernel csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasProjectRef("RimMindCore.Contracts").Should().BeTrue(
                "R-D2: Kernel must reference Contracts via ProjectReference. " +
                "Kernel uses interface types defined in Contracts.");

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeTrue(
                "R-D2: Kernel must reference Newtonsoft.Json for JSON serialization. " +
                "Kernel handles serialization of context data and prompts.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Csproj_Kernel_Should_Reference_KrafsRimworldRef()
        {
            var csprojPath = GetKernelCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Kernel csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Krafs.Rimworld.Ref").Should().BeTrue(
                "R-D2: Kernel must reference Krafs.Rimworld.Ref for Verse type access " +
                "(Pawn, Map, etc.) via Kernel abstractions.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Dll_Kernel_ShouldNot_Reference_Harmony()
        {
            if (!ArchTestExtensions.TryLocateAssembly("1_RimMindKernel.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().NotContain("0Harmony",
                "R-D2 (DLL): 1_RimMindKernel.dll must NOT reference 0Harmony. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D2_Dll_Kernel_Should_Reference_Contracts()
        {
            if (!ArchTestExtensions.TryLocateAssembly("1_RimMindKernel.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().Contain("0_RimMindContracts",
                "R-D2 (DLL): 1_RimMindKernel.dll must reference 0_RimMindContracts. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }
    }
}
