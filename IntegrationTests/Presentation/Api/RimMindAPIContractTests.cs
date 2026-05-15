using System;
using RimMind.Presentation.Runtime;

namespace RimMind.IntegrationTests.Presentation.Api
{
    public class RimMindAPIContractTests
    {
        [Fact]
        public void ClientManager_ShouldBeCreatable()
        {
            var manager = new ClientManager();
            manager.Should().NotBeNull();
        }
    }
}
