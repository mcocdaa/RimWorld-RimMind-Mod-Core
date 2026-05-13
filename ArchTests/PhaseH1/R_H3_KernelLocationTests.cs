using FluentAssertions;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Application.Features.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H3_InfrastructureLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void ToolRegistry_Should_Be_In_Application_Namespace()
        {
            typeof(ToolRegistry).Namespace.Should().StartWith("RimMind.Application",
                "ToolRegistry must be in RimMind.Application namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Be_In_Infrastructure_Mechanisms_Namespace()
        {
            typeof(GameMechanismBaseNoDef).Namespace.Should().StartWith("RimMind.Infrastructure.Mechanisms",
                "GameMechanismBaseNoDef must be in RimMind.Infrastructure.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void MechanismToolHandler_Should_Be_In_Infrastructure_Mechanisms_Namespace()
        {
            typeof(MechanismToolHandler).Namespace.Should().StartWith("RimMind.Infrastructure.Mechanisms",
                "MechanismToolHandler must be in RimMind.Infrastructure.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismRegistry_Should_Be_In_Infrastructure_Namespace()
        {
            typeof(GameMechanismRegistry).Namespace.Should().StartWith("RimMind.Infrastructure",
                "GameMechanismRegistry must be in RimMind.Infrastructure namespace");
        }
    }
}
