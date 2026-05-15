using System;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.IntegrationTests.Stubs;

namespace RimMind.IntegrationTests.Presentation.Runtime
{
    public class ClientManagerTests
    {
        [Fact]
        public void ClientManager_ShouldBeCreatable()
        {
            var manager = new ClientManager();
            manager.Should().NotBeNull();
        }
    }
}
