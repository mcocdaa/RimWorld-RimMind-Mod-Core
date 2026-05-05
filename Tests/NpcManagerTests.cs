using System.Collections.Generic;
using RimMind.Core.Npc;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    [CollectionDefinition("NpcManager", DisableParallelization = true)]
    public class NpcManagerCollection { }

    [Collection("NpcManager")]
    public class NpcManagerTests
    {
        private static void ResetStubs()
        {
            NpcManager.ClearPawnIndex();
            Find.Maps.Clear();
            Find.WorldPawns = null;
        }

        [Fact]
        public void FindPawnByNpcId_NullInput_ReturnsNull()
        {
            Assert.Null(NpcManager.FindPawnByNpcId(null));
        }

        [Fact]
        public void FindPawnByNpcId_EmptyInput_ReturnsNull()
        {
            Assert.Null(NpcManager.FindPawnByNpcId(""));
        }

        [Fact]
        public void FindPawnByNpcId_NoNpcPrefix_ReturnsNull()
        {
            Assert.Null(NpcManager.FindPawnByNpcId("123"));
        }

        [Fact]
        public void FindPawnByNpcId_InvalidIdPart_ReturnsNull()
        {
            Assert.Null(NpcManager.FindPawnByNpcId("NPC-abc"));
        }

        [Fact]
        public void FindPawnByNpcId_FoundOnMap_ReturnsPawn()
        {
            ResetStubs();
            var pawn = new Pawn { thingIDNumber = 42 };
            var map = new Map { mapPawns = new MapPawns() };
            map.mapPawns.AllPawns.Add(pawn);
            Find.Maps.Add(map);

            var result = NpcManager.FindPawnByNpcId("NPC-42");

            Assert.Same(pawn, result);
        }

        [Fact]
        public void FindPawnByNpcId_FoundOnWorldPawns_ReturnsPawn()
        {
            ResetStubs();
            var pawn = new Pawn { thingIDNumber = 99 };
            Find.WorldPawns = new WorldPawns();
            Find.WorldPawns.AllPawnsAlive.Add(pawn);

            var result = NpcManager.FindPawnByNpcId("NPC-99");

            Assert.Same(pawn, result);
        }

        [Fact]
        public void FindPawnByNpcId_MapTakesPrecedenceOverWorld()
        {
            ResetStubs();
            var mapPawn = new Pawn { thingIDNumber = 7 };
            var worldPawn = new Pawn { thingIDNumber = 7 };
            var map = new Map { mapPawns = new MapPawns() };
            map.mapPawns.AllPawns.Add(mapPawn);
            Find.Maps.Add(map);
            Find.WorldPawns = new WorldPawns();
            Find.WorldPawns.AllPawnsAlive.Add(worldPawn);

            var result = NpcManager.FindPawnByNpcId("NPC-7");

            Assert.Same(mapPawn, result);
        }

        [Fact]
        public void FindPawnByNpcId_NotFound_ReturnsNull()
        {
            ResetStubs();
            Find.Maps.Add(new Map { mapPawns = new MapPawns() });
            Find.WorldPawns = new WorldPawns();

            Assert.Null(NpcManager.FindPawnByNpcId("NPC-999"));
        }

        [Fact]
        public void FindPawnByNpcId_NullMapPawns_SkipsMap()
        {
            ResetStubs();
            var pawn = new Pawn { thingIDNumber = 10 };
            var mapWithNull = new Map { mapPawns = null };
            var mapWithPawn = new Map { mapPawns = new MapPawns() };
            mapWithPawn.mapPawns.AllPawns.Add(pawn);
            Find.Maps.Add(mapWithNull);
            Find.Maps.Add(mapWithPawn);

            var result = NpcManager.FindPawnByNpcId("NPC-10");

            Assert.Same(pawn, result);
        }

        [Fact]
        public void FindPawnByNpcId_NullWorldPawns_FallsBackGracefully()
        {
            ResetStubs();
            Find.WorldPawns = null;

            Assert.Null(NpcManager.FindPawnByNpcId("NPC-1"));
        }

        [Fact]
        public void FindPawnByNpcId_MultipleMaps_FindsCorrectOne()
        {
            ResetStubs();
            var pawn1 = new Pawn { thingIDNumber = 100 };
            var pawn2 = new Pawn { thingIDNumber = 200 };
            var map1 = new Map { mapPawns = new MapPawns() };
            map1.mapPawns.AllPawns.Add(pawn1);
            var map2 = new Map { mapPawns = new MapPawns() };
            map2.mapPawns.AllPawns.Add(pawn2);
            Find.Maps.Add(map1);
            Find.Maps.Add(map2);

            Assert.Same(pawn2, NpcManager.FindPawnByNpcId("NPC-200"));
            Assert.Same(pawn1, NpcManager.FindPawnByNpcId("NPC-100"));
        }
    }
}
