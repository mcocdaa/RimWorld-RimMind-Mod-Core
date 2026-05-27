using System;
using System.Collections.Generic;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms;

namespace RimMind.IntegrationTests
{
    /// <summary>
    /// Base class for RimMind integration tests that require RimWorld runtime.
    /// Provides TestWorld with Map + Pawn + Faction setup and teardown.
    /// </summary>
    [Collection("RimWorld Integration")]
    public abstract class TestBase : IClassFixture<TestWorldFixture>
    {
        private readonly TestWorldFixture _fixture;

        protected TestBase(TestWorldFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Access the shared TestWorld instance.
        /// </summary>
        protected TestWorld World => _fixture.World;

        /// <summary>
        /// Get the first colonist pawn from the test world.
        /// </summary>
        protected Verse.Pawn Pawn => World.GetPawn(0);

        /// <summary>
        /// Get the thingIDNumber of the first colonist pawn.
        /// </summary>
        protected int PawnId => World.GetPawnId(0);

        /// <summary>
        /// Get the test Map.
        /// </summary>
        protected Verse.Map Map => World.Map;

        /// <summary>
        /// Get the player faction.
        /// </summary>
        protected RimWorld.Faction PlayerFaction => World.PlayerFaction;

        // --- Mechanism helper methods ---

        /// <summary>
        /// Create MechanismReadArgs for a given mechanism and pawn.
        /// </summary>
        protected static MechanismReadArgs ReadArgs(string mechanismId, int pawnId, string? defName = null)
        {
            return new MechanismReadArgs
            {
                MechanismId = mechanismId,
                PawnId = pawnId,
                DefName = defName
            };
        }

        /// <summary>
        /// Create MechanismWriteArgs for a given mechanism, pawn, and action.
        /// </summary>
        protected static MechanismWriteArgs WriteArgs(
            string mechanismId,
            int pawnId,
            string action,
            string? defName = null,
            string? valueJson = null,
            Dictionary<string, string>? parms = null)
        {
            return new MechanismWriteArgs
            {
                MechanismId = mechanismId,
                PawnId = pawnId,
                Action = action,
                DefName = defName,
                ValueJson = valueJson,
                Params = parms
            };
        }

        /// <summary>
        /// Create a MechanismWriteArgs with map scope (no pawn).
        /// </summary>
        protected static MechanismWriteArgs MapWriteArgs(
            string mechanismId,
            string action,
            int? mapId = null,
            string? defName = null,
            Dictionary<string, string>? parms = null)
        {
            return new MechanismWriteArgs
            {
                MechanismId = mechanismId,
                PawnId = 0,
                MapId = mapId,
                Action = action,
                DefName = defName,
                Params = parms
            };
        }
    }
}
