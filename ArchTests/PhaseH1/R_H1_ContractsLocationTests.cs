using System.Reflection;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H1_InterfaceLocationTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IToolHandler_Should_Be_In_Application_Interfaces_Namespace()
        {
            typeof(IToolHandler).Namespace.Should().StartWith("RimMind.Application.Common.Interfaces",
                "IToolHandler must be in RimMind.Application.Common.Interfaces namespace (Jason Taylor Application layer)");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_Should_Be_In_Application_Interfaces_Namespace()
        {
            typeof(IGameMechanism).Namespace.Should().StartWith("RimMind.Application.Common.Interfaces",
                "IGameMechanism must be in RimMind.Application.Common.Interfaces namespace (Jason Taylor Application layer)");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IToolRegistry_Should_Be_In_Application_Interfaces_Namespace()
        {
            typeof(IToolRegistry).Namespace.Should().StartWith("RimMind.Application.Common.Interfaces",
                "IToolRegistry must be in RimMind.Application.Common.Interfaces namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanismRegistry_Should_Be_In_Application_Interfaces_Namespace()
        {
            typeof(IGameMechanismRegistry).Namespace.Should().StartWith("RimMind.Application.Common.Interfaces",
                "IGameMechanismRegistry must be in RimMind.Application.Common.Interfaces namespace");
        }
    }
}
