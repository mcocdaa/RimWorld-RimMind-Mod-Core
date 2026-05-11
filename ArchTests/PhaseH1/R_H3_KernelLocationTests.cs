using FluentAssertions;
using RimMind.Kernel.Mechanisms;
using RimMind.Kernel.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H3_KernelLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void ToolCallDispatchMiddleware_Should_Be_In_Kernel_Namespace()
        {
            typeof(ToolCallDispatchMiddleware).Namespace.Should().StartWith("RimMind.Kernel",
                "ToolCallDispatchMiddleware must be in RimMind.Kernel namespace, not in Core or Adapters");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void ToolRegistry_Should_Be_In_Kernel_Namespace()
        {
            typeof(ToolRegistry).Namespace.Should().StartWith("RimMind.Kernel",
                "ToolRegistry must be in RimMind.Kernel namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Be_In_Kernel_Mechanisms_Namespace()
        {
            typeof(GameMechanismBaseNoDef).Namespace.Should().StartWith("RimMind.Kernel.Mechanisms",
                "GameMechanismBaseNoDef must be in RimMind.Kernel.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void MechanismToolHandler_Should_Be_In_Kernel_Mechanisms_Namespace()
        {
            typeof(MechanismToolHandler).Namespace.Should().StartWith("RimMind.Kernel.Mechanisms",
                "MechanismToolHandler must be in RimMind.Kernel.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismRegistry_Should_Be_In_Kernel_Mechanisms_Namespace()
        {
            typeof(GameMechanismRegistry).Namespace.Should().StartWith("RimMind.Kernel.Mechanisms",
                "GameMechanismRegistry must be in RimMind.Kernel.Mechanisms namespace");
        }
    }
}
