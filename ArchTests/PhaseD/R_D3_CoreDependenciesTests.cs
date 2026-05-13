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
                "it loads third (after Domain and Application) in RimWorld's alphabetical assembly loading order.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_Should_Reference_Application()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            analysis.HasProjectRef("RimMindCore.Application").Should().BeTrue(
                "R-D3: Core must reference Application via ProjectReference. " +
                "Core uses Application services (ContextEngine, AgentBus, etc.).");
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
                "Core contains Infrastructure/Patches which use HarmonyPatch.");

            analysis.HasPackageRef("Newtonsoft.Json").Should().BeTrue(
                "R-D3: Core must reference Newtonsoft.Json. " +
                "Core contains Client layer which uses JSON for AI API communication.");

            analysis.HasPackageRef("Krafs.Rimworld.Ref").Should().BeTrue(
                "R-D3: Core must reference Krafs.Rimworld.Ref. " +
                "Core contains Infrastructure/Verse which directly interacts with RimWorld types.");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_Excludes_Domain_And_Application_Source()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var content = File.ReadAllText(csprojPath);

            content.Should().NotContain("<Compile Include=\"Domain\\",
                "R-D3: Core csproj must NOT include Domain source files. " +
                "Domain is compiled as a separate assembly (0_RimMindDomain.dll).");

            content.Should().NotContain("<Compile Include=\"Application\\",
                "R-D3: Core csproj must NOT include Application source files. " +
                "Application is compiled as a separate assembly (1_RimMindApplication.dll).");
        }

        [Fact]
        [Trait("Phase", "D")]
        public void R_D3_Csproj_Core_References_Complete()
        {
            var csprojPath = GetCoreCsprojPath();
            File.Exists(csprojPath).Should().BeTrue($"Core csproj must exist at {csprojPath}");

            var analysis = ArchTestExtensions.AnalyzeCsproj(csprojPath);

            var expectedProjectRefs = new[] { "RimMindCore.Application" };
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
        public void R_D3_Dll_Core_Should_Reference_Domain_And_Application()
        {
            if (!ArchTestExtensions.TryLocateAssembly("2_RimMindCore.dll", out var dllPath))
            {
                return;
            }

            var refs = ArchTestExtensions.GetAssemblyReferences(dllPath!);

            refs.Should().Contain("0_RimMindDomain",
                "R-D3 (DLL): 2_RimMindCore.dll must reference 0_RimMindDomain. " +
                $"Actual references: {string.Join(", ", refs.OrderBy(r => r))}");

            refs.Should().Contain("1_RimMindApplication",
                "R-D3 (DLL): 2_RimMindCore.dll must reference 1_RimMindApplication. " +
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

            var domainCsproj = Path.Combine(sourceDir, "Domain", "RimMindCore.Domain.csproj");
            var applicationCsproj = Path.Combine(sourceDir, "Application", "RimMindCore.Application.csproj");
            var coreCsproj = Path.Combine(sourceDir, "RimMindCore.csproj");

            File.Exists(domainCsproj).Should().BeTrue("Domain csproj must exist");
            File.Exists(applicationCsproj).Should().BeTrue("Application csproj must exist");
            File.Exists(coreCsproj).Should().BeTrue("Core csproj must exist");

            var domainAnalysis = ArchTestExtensions.AnalyzeCsproj(domainCsproj);
            var applicationAnalysis = ArchTestExtensions.AnalyzeCsproj(applicationCsproj);
            var coreAnalysis = ArchTestExtensions.AnalyzeCsproj(coreCsproj);

            var assemblyNames = new[] { domainAnalysis.AssemblyName, applicationAnalysis.AssemblyName, coreAnalysis.AssemblyName };
            var sorted = assemblyNames.OrderBy(n => n).ToList();

            sorted[0].Should().StartWith("0_",
                "First-loaded assembly must have '0_' prefix (Domain)");
            sorted[1].Should().StartWith("1_",
                "Second-loaded assembly must have '1_' prefix (Application)");
            sorted[2].Should().StartWith("2_",
                "Third-loaded assembly must have '2_' prefix (Core)");
        }
    }
}
