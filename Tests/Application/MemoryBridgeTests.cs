using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Memory;
using RimMind.Application.Common.Models.Memory;
using RimMind.Presentation.Api;
using Xunit;

namespace RimMind.Tests.Application
{
    public class MemoryBridgeTests
    {
        [Fact]
        public void DefaultBridge_IsSafeNoOp()
        {
            RimMindAPI.Memory.RegisterBridge(new NullMemoryBridge());

            Assert.False(RimMindAPI.Memory.AddPawnMemory("event", MemoryKind.Event, 10, 0.5f, "pawn-1"));
            Assert.False(RimMindAPI.Memory.AddNarratorMemory("narration", 10, 0.5f));
            Assert.Empty(RimMindAPI.Memory.GetRecentNarrations(5));
        }

        [Fact]
        public void RegisteredBridge_ReceivesTypedMemoryOperations()
        {
            var bridge = new RecordingMemoryBridge();
            RimMindAPI.Memory.RegisterBridge(bridge);

            bool pawnAdded = RimMindAPI.Memory.AddPawnMemory("event", MemoryKind.Event, 42, 0.75f, "pawn-9");
            bool narratorAdded = RimMindAPI.Memory.AddNarratorMemory("narration", 43, 0.25f);
            var narrations = RimMindAPI.Memory.GetRecentNarrations(3);

            Assert.True(pawnAdded);
            Assert.True(narratorAdded);
            Assert.Equal(("event", MemoryKind.Event, 42, 0.75f, "pawn-9"), bridge.LastPawnRequest);
            Assert.Equal(("narration", 43, 0.25f), bridge.LastNarratorRequest);
            Assert.Equal(new NarratorMemoryEntry("narration", 43), Assert.Single(narrations));

            RimMindAPI.Memory.RegisterBridge(new NullMemoryBridge());
        }

        private sealed class RecordingMemoryBridge : IMemoryBridge
        {
            public (string Content, MemoryKind Kind, int Tick, float Importance, string? PawnId) LastPawnRequest { get; private set; }
            public (string Content, int Tick, float Importance) LastNarratorRequest { get; private set; }

            public bool AddPawnMemory(string content, MemoryKind kind, int tick, float importance, string? pawnId)
            {
                LastPawnRequest = (content, kind, tick, importance, pawnId);
                return true;
            }

            public bool AddNarratorMemory(string content, int tick, float importance)
            {
                LastNarratorRequest = (content, tick, importance);
                return true;
            }

            public IReadOnlyList<NarratorMemoryEntry> GetRecentNarrations(int maxEntries)
                => new[] { new NarratorMemoryEntry("narration", 43) };
        }
    }
}
