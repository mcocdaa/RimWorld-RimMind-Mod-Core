using FluentAssertions;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Infrastructure.Mechanisms;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H5b_RegistryLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanismRegistry_Should_Be_In_Application_Interfaces()
        {
            typeof(IGameMechanismRegistry).Namespace.Should().StartWith("RimMind.Application.Common.Interfaces",
                "IGameMechanismRegistry interface must be in RimMind.Application.Common.Interfaces namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismRegistry_Should_Be_In_Infrastructure()
        {
            typeof(GameMechanismRegistry).Namespace.Should().StartWith("RimMind.Infrastructure",
                "GameMechanismRegistry implementation must be in RimMind.Infrastructure namespace");
        }
    }
}
