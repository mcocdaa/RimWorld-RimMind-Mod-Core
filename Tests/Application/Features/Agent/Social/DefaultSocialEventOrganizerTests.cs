using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Features.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Social
{
    public class DefaultSocialEventOrganizerTests
    {
        private readonly StubTickProvider _tick = new();
        private readonly StubAgentBus _bus = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };

        private DefaultSocialEventOrganizer CreateOrganizer()
            => new(_tick, _bus);

        [Fact]
        public void ShouldOrganize_ReturnsFalse()
        {
            var organizer = CreateOrganizer();
            Assert.False(organizer.ShouldOrganize(_agent));
        }

        [Fact]
        public async Task OrganizeAsync_ReturnsErr()
        {
            var organizer = CreateOrganizer();
            var result = await organizer.OrganizeAsync(_agent);
            Assert.True(result.IsErr);
        }

        [Fact]
        public void GetPendingEvents_ReturnsEmpty()
        {
            var organizer = CreateOrganizer();
            var events = organizer.GetPendingEvents();
            Assert.Empty(events);
        }

        [Fact]
        public void MarkEventExecuted_DoesNotThrow()
        {
            var organizer = CreateOrganizer();
            var ex = Record.Exception(() => organizer.MarkEventExecuted("event-1"));
            Assert.Null(ex);
        }
    }
}
