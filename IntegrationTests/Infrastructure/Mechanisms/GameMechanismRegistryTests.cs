using System;
using RimMind.Infrastructure.Mechanisms;

namespace RimMind.IntegrationTests.Infrastructure.Mechanisms
{
    public class GameMechanismRegistryTests
    {
        [Fact]
        public void Registry_ShouldBeCreatable()
        {
            var registry = new GameMechanismRegistry();
            registry.Should().NotBeNull();
        }

        [Fact]
        public void Registry_All_ShouldReturnNotNull()
        {
            var registry = new GameMechanismRegistry();
            var mechanisms = registry.All;
            mechanisms.Should().NotBeNull();
        }
    }
}
