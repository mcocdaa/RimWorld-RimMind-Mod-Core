using UnityEngine;
using Xunit;
using RimMind.Presentation.UI.Layout;

namespace RimMind.Tests.Infrastructure.UI.Layout
{
    public class LayoutConflictTests
    {
        [Fact]
        public void ConflictKind_HasThreeValues()
        {
            Assert.True(System.Enum.IsDefined(typeof(ConflictKind), ConflictKind.Overlap));
            Assert.True(System.Enum.IsDefined(typeof(ConflictKind), ConflictKind.Overflow));
            Assert.True(System.Enum.IsDefined(typeof(ConflictKind), ConflictKind.NegativeSize));
        }

        [Fact]
        public void LayoutConflict_Factory_Builds_Overlap()
        {
            var a = new LayoutTraceEntry(new Rect(0, 0, 10, 10), "a", "srcA");
            var b = new LayoutTraceEntry(new Rect(5, 5, 10, 10), "b", "srcB");
            var c = LayoutConflict.Overlap(a, b);
            Assert.Equal(ConflictKind.Overlap, c.Kind);
            Assert.Equal(2, c.Entries.Length);
            Assert.Contains(a, c.Entries);
            Assert.Contains(b, c.Entries);
        }

        [Fact]
        public void LayoutConflict_Factory_Builds_Overflow()
        {
            var entry = new LayoutTraceEntry(new Rect(0, 0, 10, 10), "x", "srcX");
            var c = LayoutConflict.Overflow(entry, viewRect: new Rect(0, 0, 100, 100), overflowBottom: 110f);
            Assert.Equal(ConflictKind.Overflow, c.Kind);
            Assert.Equal(110f, c.OverflowBottom);
        }

        [Fact]
        public void LayoutConflict_Factory_Builds_NegativeSize()
        {
            var entry = new LayoutTraceEntry(new Rect(0, 0, -5, 10), "neg", "srcN");
            var c = LayoutConflict.NegativeSize(entry);
            Assert.Equal(ConflictKind.NegativeSize, c.Kind);
            Assert.Equal(entry, c.Entries[0]);
        }
    }
}
