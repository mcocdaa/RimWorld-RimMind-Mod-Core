using System.Collections.Generic;
using UnityEngine;
using Xunit;
using RimMind.Infrastructure.UI.Layout;

namespace RimMind.Tests.Infrastructure.UI.Layout
{
    public class LayoutTraceRecorderTests
    {
        private static LayoutTraceRecorder NewRecorder(float viewW = 100f, float viewH = 100f)
            => new(new Rect(0f, 0f, viewW, viewH));

        [Fact]
        public void DetectConflicts_Empty_ReturnsNoConflicts()
        {
            var r = NewRecorder();
            Assert.Empty(r.DetectConflicts());
        }

        [Fact]
        public void DetectConflicts_NegativeSize_IsFlagged()
        {
            var r = NewRecorder();
            r.Record(new Rect(0, 0, -5, 10), "neg", "src");
            var conflicts = r.DetectConflicts();
            Assert.Single(conflicts);
            Assert.Equal(ConflictKind.NegativeSize, conflicts[0].Kind);
        }

        [Fact]
        public void DetectConflicts_Overflow_IsFlagged()
        {
            var r = NewRecorder(viewH: 50f);
            r.Record(new Rect(0, 40, 10, 20), "overflow", "src"); // yMax=60 > 50
            var conflicts = r.DetectConflicts();
            Assert.Single(conflicts);
            Assert.Equal(ConflictKind.Overflow, conflicts[0].Kind);
        }

        [Fact]
        public void DetectConflicts_Overlap_IsFlagged()
        {
            var r = NewRecorder();
            r.Record(new Rect(0, 0, 10, 10), "a", "srcA");
            r.Record(new Rect(5, 5, 10, 10), "b", "srcB");
            var conflicts = r.DetectConflicts();
            Assert.Single(conflicts);
            Assert.Equal(ConflictKind.Overlap, conflicts[0].Kind);
        }

        [Fact]
        public void DetectConflicts_AdjacentRects_DoNotOverlap()
        {
            // Adjacent (share an edge but no interior intersection) must NOT be flagged
            var r = NewRecorder();
            r.Record(new Rect(0, 0, 10, 10), "a", "srcA");
            r.Record(new Rect(10, 0, 10, 10), "b", "srcB"); // starts at a.xMax
            Assert.Empty(r.DetectConflicts());
        }

        [Fact]
        public void DetectConflicts_Touching_Vertically_DoNotOverlap()
        {
            var r = NewRecorder();
            r.Record(new Rect(0, 0, 10, 10), "a", "srcA");
            r.Record(new Rect(0, 10, 10, 10), "b", "srcB"); // starts at a.yMax
            Assert.Empty(r.DetectConflicts());
        }

        [Fact]
        public void DetectConflicts_Overflow_ToleratesEpsilon()
        {
            // 0.5f sub-pixel overflow should be tolerated (Unity GUI float noise)
            var r = NewRecorder(viewH: 50f);
            r.Record(new Rect(0, 30, 10, 20.4f), "near", "src"); // yMax=50.4
            Assert.Empty(r.DetectConflicts());
        }

        [Fact]
        public void DetectConflicts_MultipleOverlaps_ReturnsEachPair()
        {
            var r = NewRecorder(viewW: 200f, viewH: 200f);
            r.Record(new Rect(0, 0, 50, 50), "a", "srcA");
            r.Record(new Rect(10, 10, 50, 50), "b", "srcB");
            r.Record(new Rect(20, 20, 50, 50), "c", "srcC");
            // (a,b), (a,c), (b,c) — three overlap pairs
            var conflicts = r.DetectConflicts();
            Assert.Equal(3, conflicts.Count);
        }

        [Fact]
        public void Reset_ClearsEntries()
        {
            var r = NewRecorder();
            r.Record(new Rect(0, 0, 10, 10), "a", "src");
            r.Reset();
            Assert.Empty(r.DetectConflicts());
        }

        [Fact]
        public void ViewRect_IsExposed_ForConsumers()
        {
            var view = new Rect(1, 2, 3, 4);
            var r = new LayoutTraceRecorder(view);
            Assert.Equal(view, r.ViewRect);
        }
    }
}
