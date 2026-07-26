using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    internal sealed class StubNpcManager : INpcManager
    {
        public bool IsAliveResult { get; set; } = true;
        public NpcProfile? ProfileResult { get; set; }
        public int SpawnCount { get; private set; }

        public bool IsNpcAlive(string npcId) => IsAliveResult;
        public NpcProfile? GetNpc(string npcId) => ProfileResult;
        public void SpawnNpc(NpcProfile profile) => SpawnCount++;
        public void KillNpc(string npcId) { }
        public IReadOnlyList<NpcProfile> GetAllNpcs() => new List<NpcProfile>().AsReadOnly();
        public string GetNpcForMap(object map) => "";
        public object? FindPawnByNpcId(string npcId) => null;
        public object? FindProxyPawnForMap(object map) => null;
        public void RegisterActiveAgent(int thingId) { }
        public void UnregisterActiveAgent(int thingId) { }
        public HashSet<int> GetActiveAgentPawnIds() => new HashSet<int>();
        public void IndexPawn(object pawn) { }
        public void UnindexPawn(int thingId) { }
        public string GetMapNpcId(object map) => "";
    }

    internal sealed class StubNpcManagerAccessor : INpcManagerAccessor
    {
        public StubNpcManagerAccessor(INpcManager current) => Current = current;
        public INpcManager? Current { get; }
    }

    public class NpcEnrichMiddlewareTests
    {
        [Fact]
        public async Task NoNpcId_CallsNextDirectly()
        {
            var middleware = new NpcEnrichMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                },
            };
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task NpcNotAlive_WithProfile_Respawns()
        {
            var profile = new NpcProfile { NpcId = "npc-1" };
            var npcManager = new StubNpcManager
            {
                IsAliveResult = false,
                ProfileResult = profile,
            };
            var middleware = new NpcEnrichMiddleware(
                npcManagers: new StubNpcManagerAccessor(npcManager));
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.Equal(1, npcManager.SpawnCount);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task NpcNotAlive_NoProfile_ShortCircuits()
        {
            var npcManager = new StubNpcManager
            {
                IsAliveResult = false,
                ProfileResult = null,
            };
            var middleware = new NpcEnrichMiddleware(
                npcManagers: new StubNpcManagerAccessor(npcManager));
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("npc_not_found", context.ShortCircuitReason);
        }

        [Fact]
        public async Task NpcAlive_InjectsProfileIntoItems()
        {
            var profile = new NpcProfile { NpcId = "npc-1" };
            var npcManager = new StubNpcManager
            {
                IsAliveResult = true,
                ProfileResult = profile,
            };
            var middleware = new NpcEnrichMiddleware(
                npcManagers: new StubNpcManagerAccessor(npcManager));
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    NpcId = "npc-1",
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.Items.ContainsKey("NpcProfile"));
            Assert.Same(profile, context.Items["NpcProfile"]);
        }
    }
}
