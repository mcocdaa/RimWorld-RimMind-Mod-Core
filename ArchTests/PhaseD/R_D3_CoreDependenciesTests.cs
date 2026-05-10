using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseD
{
    public class CoreDependenciesTests
    {
        private static string GetCoreCsprojPath()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            return Path.Combine(sourceDir, "RimMindCore.csproj");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_AssemblyName_ShouldBe_Prefixed()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.AssemblyName.Should().Be("2_RimMindCore",
                "R-D3: Core assembly name must be '2_RimMindCore' to ensure " +
                "it loads third (after Contracts and Kernel) in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_Should_Reference_Contracts_And_Kernel()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasProjectRef("RimMindCore.Contracts").Should().BeTrue(
                "R-D3: Core must reference Contracts via ProjectReference. " +
                "Core implements interfaces defined in Contracts.");

            analysis.HasProjectRef("RimMindCore.Kernel").Should().BeTrue(
                "R-D3: Core must reference Kernel via ProjectReference. " +
                "Core uses Kernel services (ContextEngine, AgentBus, etc.).");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_Should_Reference_Harmony_And_Newtonsoft()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasPackageRef("Lib.Harmony.Ref").Should().BeTrue(
                "R-D3: Core must reference Lib.Harmony.Ref. " +
                "Core contains Adapters/Patches which use HarmonyPatch.");

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeTrue(
                "R-D3: Core must reference Newtonsoft.Json. " +
                "Core contains Client layer which uses JSON for AI API communication.");

            analysis.HasPackageRef("Krafs.Rimworld.Ref").Should().BeTrue(
                "R-D3: Core must reference Krafs.Rimworld.Ref. " +
                "Core contains Adapters/Verse which directly interact with RimWorld types.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_Excludes_Contracts_And_Kernel_Source()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var content = File.ReadAllText(csprojPath);

            content.Should().NotContain("<Compile Include=\"Contracts\\",
                "R-D3: Core csproj must NOT include Contracts source files. " +
                "Contracts is compiled as a separate assembly (0_RimMindContracts.dll).");

            content.Should().NotContain(@"<Compile Include=""Kernel\**\*.cs""",
                "R-D3: Core csproj must NOT include Kernel source files via wildcard. " +
                "Kernel is compiled as a separate assembly (1_RimMindKernel.dll). " +
                "Individual files that depend on Core types may be included as exceptions.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_References_Complete()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            var expectedProjectRefs = new[] { "RimMindCore.Contracts", "RimMindCore.Kernel" };
            var expectedPackageRefs = new[] { "Krafs.Rimworld.Ref", "Lib.Harmony.Ref", "Newtonsoft.Json" };

            var missingProjectRefs = expectedProjectRefs
                .Where(name => !analysis.HasProjectRef(name))
                .ToList();

            var missingPackageRefs = expectedPackageRefs
                .Where(name => !analysis.HasPackageRef(name))
                .ToList();

            var errors = new List<string>();
            if (missingProjectRefs.Any())
                errors.Add($"Missing ProjectReferences: {string.Join(", ", missingProjectRefs)}");
            if (missingPackageRefs.Any())
                errors.Add($"Missing PackageReferences: {string.Join(", ", missingPackageRefs)}");

            errors.Should().BeEmpty(
                "R-D3: 2_RimMindCore.dll must have complete references. " +
                string.Join("; ", errors));
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Dll_Core_Should_Reference_Contracts_And_Kernel()
        {
            if (!ArchTestExtensions.TryLocateAssembly("2_RimMindCore.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().Contain("0_RimMindContracts",
                "R-D3 (DLL): 2_RimMindCore.dll must reference 0_RimMindContracts. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");

            refs.Should().Contain("1_RimMindKernel",
                "R-D3 (DLL): 2_RimMindCore.dll must reference 1_RimMindKernel. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Dll_Core_Should_Reference_Harmony_And_Newtonsoft()
        {
            if (!ArchTestExtensions.TryLocateAssembly("2_RimMindCore.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().Contain("0Harmony",
                "R-D3 (DLL): 2_RimMindCore.dll must reference 0Harmony. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");

            refs.Should().Contain("Newtonsoft.Json",
                "R-D3 (DLL): 2_RimMindCore.dll must reference Newtonsoft.Json. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Loading_Order_Three_Assemblies_Exist()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeEmpty("Source directory must exist");

            var contractsCsproj = Path.Combine(sourceDir, "Contracts", "RimMindCore.Contracts.csproj");
            var kernelCsproj = Path.Combine(sourceDir, "Kernel", "RimMindCore.Kernel.csproj");
            var coreCsproj = Path.Combine(sourceDir, "RimMindCore.csproj");

            File.Exists(contractsCsproj).Should().BeTrue("Contracts csproj must exist");
            File.Exists(kernelCsproj).Should().BeTrue("Kernel csproj must exist");
            File.Exists(coreCsproj).Should().BeTrue("Core csproj must exist");

            var contractsAnalysis = ArchTestExtensions.AnalyzeCsproj(contractsCsproj);
            var kernelAnalysis = ArchTestExtensions.AnalyzeCsproj(kernelCsproj);
            var coreAnalysis = ArchTestExtensions.AnalyzeCsproj(coreCsproj);

            var assemblyNames = new[] { contractsAnalysis.AssemblyName, kernelAnalysis.AssemblyName, coreAnalysis.AssemblyName };
            var sorted = assemblyNames.OrderBy(n => n).ToList();

            sorted[0].Should().StartWith("0_",
                "First-loaded assembly must have '0_' prefix (Contracts)");
            sorted[1].Should().StartWith("1_",
                "Second-loaded assembly must have '1_' prefix (Kernel)");
            sorted[2].Should().StartWith("2_",
                "Third-loaded assembly must have '2_' prefix (Core)");
        }
    }
}
