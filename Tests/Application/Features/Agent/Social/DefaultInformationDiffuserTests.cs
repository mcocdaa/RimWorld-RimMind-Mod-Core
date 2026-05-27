using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Features.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Events;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.Social
{
    public class DefaultInformationDiffuserTests
    {
        private readonly StubTickProvider _tick = new() { TicksGame = 1000 };
        private readonly StubAgentBus _bus = new();
        private readonly StubAgentInfo _source = new() { NpcId = "npc-source" };
        private readonly StubAgentInfo _target = new() { NpcId = "npc-target" };

        private DefaultInformationDiffuser CreateDiffuser()
            => new(_bus, _tick);

        private RumorEntry MakeRumor(float importance = 0.5f, int distortionLevel = 0)
            => new()
            {
                RumorId = "rumor-1",
                Content = "A rumor",
                SourceNpcId = "npc-origin",
                Importance = importance,
                CreatedTick = 500,
                DistortionLevel = distortionLevel,
            };

        [Fact]
        public void ShouldDiffuse_ImportanceZero_AlwaysReturnsFalse()
        {
            var diffuser = CreateDiffuser();
            var rumor = MakeRumor(importance: 0f);
            // probability = 0 * 0.7 * 0.5 = 0, random.NextDouble() is [0,1) so always >= 0
            var results = Enumerable.Range(0, 100)
                .Select(_ => diffuser.ShouldDiffuse(_source, _target, rumor))
                .ToList();
            Assert.All(results, r => Assert.False(r));
        }

        [Fact]
        public void AddRumor_GetKnownRumors_StoresAndRetrievesCorrectly()
        {
            var diffuser = CreateDiffuser();
            var rumor = MakeRumor();
            diffuser.AddRumor("npc-target", rumor);

            var rumors = diffuser.GetKnownRumors("npc-target");
            Assert.Single(rumors);
            Assert.Equal("rumor-1", rumors[0].RumorId);
            Assert.Equal("A rumor", rumors[0].Content);
        }

        [Fact]
        public void GetKnownRumors_NonExistentNpcId_ReturnsEmptyList()
        {
            var diffuser = CreateDiffuser();
            var rumors = diffuser.GetKnownRumors("non-existent");
            Assert.Empty(rumors);
        }

        [Fact]
        public void Diffuse_PublishesInformationDiffusionEvent()
        {
            var diffuser = CreateDiffuser();
            var rumor = MakeRumor(importance: 0.8f, distortionLevel: 0);

            var result = diffuser.Diffuse(_source, _target, rumor);

            Assert.True(result.IsOk);
            var diffused = result.Value;
            Assert.Equal(1, diffused.DistortionLevel);
            Assert.Equal("npc-source", diffused.SourceNpcId);

            // Verify event was published
            Assert.Single(_bus.PublishedEvents);
            var evt = Assert.IsType<InformationDiffusionEvent>(_bus.PublishedEvents[0]);
            Assert.Equal("npc-target", evt.NpcId);
            Assert.Equal("rumor-1", evt.RumorId);
            Assert.Equal("A rumor", evt.Content);
            Assert.Equal("npc-source", evt.SourceNpcId);
            Assert.Equal(0.8f, evt.Importance);
            Assert.Equal(1, evt.DistortionLevel);
        }

        [Fact]
        public void Diffuse_AddsRumorToTarget()
        {
            var diffuser = CreateDiffuser();
            var rumor = MakeRumor(importance: 0.8f);

            diffuser.Diffuse(_source, _target, rumor);

            var rumors = diffuser.GetKnownRumors("npc-target");
            Assert.Single(rumors);
            Assert.Equal(1, rumors[0].DistortionLevel);
            Assert.Equal("npc-source", rumors[0].SourceNpcId);
        }

        [Fact]
        public void Diffuse_IncrementsDistortionLevel()
        {
            var diffuser = CreateDiffuser();
            var rumor = MakeRumor(importance: 0.8f, distortionLevel: 2);

            var result = diffuser.Diffuse(_source, _target, rumor);

            Assert.True(result.IsOk);
            Assert.Equal(3, result.Value.DistortionLevel);
        }
    }
}
