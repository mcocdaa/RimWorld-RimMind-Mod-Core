using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using VersePawn = Verse.Pawn;
using VerseMap = Verse.Map;

namespace RimMind.IntegrationTests
{
    /// <summary>
    /// Factory for constructing minimal RimWorld game state for integration tests.
    /// Uses the existing game map and generates test pawns.
    /// Must run within the RimWorld game process (via DebugAction or game-loaded test runner).
    /// </summary>
    public sealed class TestWorld : IDisposable
    {
        private readonly List<VersePawn> _pawns = new();
        private VerseMap? _map;
        private bool _disposed;

        public VerseMap Map => _map ?? throw new InvalidOperationException("TestWorld not initialized");
        public IReadOnlyList<VersePawn> Pawns => _pawns.AsReadOnly();
        public Faction PlayerFaction => Faction.OfPlayer;

        /// <summary>
        /// Get a pawn by index. Default returns the first colonist.
        /// </summary>
        public VersePawn GetPawn(int index = 0)
        {
            if (index < 0 || index >= _pawns.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Pawn index {index} out of range (0-{_pawns.Count - 1})");
            return _pawns[index];
        }

        /// <summary>
        /// Get the thingIDNumber of a pawn by index.
        /// </summary>
        public int GetPawnId(int index = 0) => GetPawn(index).thingIDNumber;

        /// <summary>
        /// Create a minimal test world using the current game map and generating colonist pawns.
        /// Requires RimWorld runtime (game assemblies loaded).
        /// </summary>
        public static TestWorld Create(int colonistCount = 1)
        {
            var world = new TestWorld();
            world.Initialize(colonistCount);
            return world;
        }

        private void Initialize(int colonistCount)
        {
            // Verify game runtime is loaded
            EnsureFindRoot();

            // Use existing map from the game
            _map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (_map == null)
                throw new InvalidOperationException(
                    "No map available. Ensure a game is loaded with at least one map before running integration tests.");

            // Generate colonist pawns
            for (int i = 0; i < colonistCount; i++)
            {
                var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                if (pawn != null)
                {
                    GenPlace.TryPlaceThing(pawn, _map.Center, _map, ThingPlaceMode.Near);
                    _pawns.Add(pawn);
                }
            }

            if (_pawns.Count == 0)
                throw new InvalidOperationException("Failed to generate any colonist pawns.");
        }

        private static void EnsureFindRoot()
        {
            if (Find.Root == null)
            {
                throw new InvalidOperationException(
                    "Find.Root is null. Integration tests must run within the RimWorld game process " +
                    "(via DebugAction) or with game assemblies loaded via RIMWORLD_PATH.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Clean up generated pawns
            foreach (var pawn in _pawns)
            {
                try { pawn.Destroy(); }
                catch { /* best effort */ }
            }
            _pawns.Clear();

            // Do NOT dispose the map - it belongs to the game
            _map = null;
        }
    }
}
