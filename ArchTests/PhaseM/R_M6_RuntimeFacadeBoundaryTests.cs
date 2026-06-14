using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseM;

public sealed class R_M6_RuntimeFacadeBoundaryTests
{
    [Fact]
    [Trait("Phase", "M")]
    public void R_M6_Runtime_Facade_Should_Not_Expose_Global_Service_Registration()
    {
        string sourceDir = ArchTestExtensions.FindSourceDirectory();
        string runtimePath = Path.Combine(sourceDir, "Presentation", "Runtime", "RimMindRuntime.cs");
        string runtimeInterfacePath = Path.Combine(sourceDir, "Application", "Common", "Interfaces", "Runtime", "IRimMindRuntime.cs");

        File.ReadAllText(runtimePath).Should().NotContain("RegisterService",
            "CLEAN_ARCH_ERROR R-M6-RUNTIME-FACADE: Runtime facade must not expose global service registration. Violating file: {0}. Fix: register services from RimMindCompositionRoot or a Verse lifecycle adapter only.",
            runtimePath);
        File.ReadAllText(runtimeInterfacePath).Should().NotContain("RegisterService",
            "CLEAN_ARCH_ERROR R-M6-RUNTIME-FACADE: Runtime interface must not expose global service registration. Violating file: {0}. Fix: expose explicit runtime capabilities instead of a service-locator write API.",
            runtimeInterfacePath);
    }
}
