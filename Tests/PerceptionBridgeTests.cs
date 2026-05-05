using System.Collections.Generic;
using RimMind.Core.AgentBus;
using RimMind.Core.Comps;
using RimMind.Core.Perception;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PerceptionBridgeTests
    {
        private readonly List<PerceptionEvent> _published = new List<PerceptionEvent>();

        public PerceptionBridgeTests()
        {
            ResetStubs();
            AgentBus.AgentBus.Subscribe<PerceptionEvent>(e => _published.Add(e));
        }

        private static void ResetStubs()
        {
            Find.Maps = new List<Map>();
            CompPawnAgent.ActivePawnIds.Clear();
        }

        [Fact]
        public void PublishPerceptionForPawn_NullPawn_DoesNotPublish()
        {
            PerceptionBridge.PublishPerceptionForPawn(null!, "test", "content");
            Assert.Empty(_published);
        }

        [Fact]
        public void PublishPerceptionForPawn_PawnMapNull_DoesNotPublish()
        {
            var pawn = new Pawn { thingIDNumber = 1, Map = null };
            CompPawnAgent.ActivePawnIds.Add(1);
            PerceptionBridge.PublishPerceptionForPawn(pawn, "test", "content");
            Assert.Empty(_published);
        }

        [Fact]
        public void PublishPerceptionForPawn_InactiveAgent_DoesNotPublish()
        {
            var map = new Map();
            var pawn = new Pawn { thingIDNumber = 2, Map = map };
            PerceptionBridge.PublishPerceptionForPawn(pawn, "test", "content");
            Assert.Empty(_published);
        }

        [Fact]
        public void PublishBroadcast_FindMapsNull_DoesNotThrow()
        {
            Find.Maps = null!;
            PerceptionBridge.PublishBroadcast("test", "content");
            Assert.Empty(_published);
        }

        [Fact]
        public void PublishBroadcast_NullMapPawns_SkipsMap()
        {
            var map = new Map { mapPawns = null };
            Find.Maps.Add(map);
            PerceptionBridge.PublishBroadcast("test", "content");
            Assert.Empty(_published);
        }

        [Fact]
        public void PublishBroadcast_InactiveAgent_SkipsPawn()
        {
            var map = new Map { mapPawns = new MapPawns() };
            var pawn = new Pawn { thingIDNumber = 30 };
            map.mapPawns.FreeColonistsAndPrisoners.Add(pawn);
            Find.Maps.Add(map);

            PerceptionBridge.PublishBroadcast("event", "msg");

            Assert.Empty(_published);
        }
    }
}
