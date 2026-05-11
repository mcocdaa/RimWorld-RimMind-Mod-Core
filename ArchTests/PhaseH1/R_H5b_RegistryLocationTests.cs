using FluentAssertions;
using RimMind.Contracts.Mechanisms;
using RimMind.Kernel.Mechanisms;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H5b_RegistryLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanismRegistry_Should_Be_In_Contracts()
        {
            typeof(IGameMechanismRegistry).Namespace.Should().StartWith("RimMind.Contracts",
                "IGameMechanismRegistry interface must be in RimMind.Contracts namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismRegistry_Should_Be_In_Kernel()
        {
            typeof(GameMechanismRegistry).Namespace.Should().StartWith("RimMind.Kernel",
                "GameMechanismRegistry implementation must be in RimMind.Kernel namespace");
        }
    }
}
