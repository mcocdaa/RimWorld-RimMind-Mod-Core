using System.Reflection;
using FluentAssertions;
using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H1_ContractsLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IToolHandler_Should_Be_In_Contracts_Namespace()
        {
            typeof(IToolHandler).Namespace.Should().StartWith("RimMind.Contracts",
                "IToolHandler must be in RimMind.Contracts namespace, not in Kernel or Core");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_Should_Be_In_Contracts_Namespace()
        {
            typeof(IGameMechanism).Namespace.Should().StartWith("RimMind.Contracts",
                "IGameMechanism must be in RimMind.Contracts namespace, not in Kernel or Core");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IToolRegistry_Should_Be_In_Contracts_Namespace()
        {
            typeof(IToolRegistry).Namespace.Should().StartWith("RimMind.Contracts",
                "IToolRegistry must be in RimMind.Contracts namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanismRegistry_Should_Be_In_Contracts_Namespace()
        {
            typeof(IGameMechanismRegistry).Namespace.Should().StartWith("RimMind.Contracts",
                "IGameMechanismRegistry must be in RimMind.Contracts namespace");
        }
    }
}
