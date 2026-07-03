using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xunit;
using RimMind.Infrastructure.UI.Layout;

namespace RimMind.Tests.Infrastructure.UI.Layout
{
    public class LayoutConflictStoreTests
    {
        [Fact]
        public void Publish_Then_TryGet_ReturnsLatest()
        {
            LayoutConflictStore.Clear();
            var report1 = new LayoutReport("WinA", new List<LayoutConflict>());
            LayoutConflictStore.Publish(report1);
            var report2 = new LayoutReport("WinA", new List<LayoutConflict>
            {
                LayoutConflict.NegativeSize(new LayoutTraceEntry(new Rect(0,0,-1,10), "x", "src"))
            });
            LayoutConflictStore.Publish(report2);

            Assert.True(LayoutConflictStore.TryGet("WinA", out var latest));
            Assert.Same(report2, latest);
            Assert.Single(latest.Conflicts);
        }

        [Fact]
        public void TryGet_UnknownWindow_ReturnsFalse()
        {
            LayoutConflictStore.Clear();
            Assert.False(LayoutConflictStore.TryGet("DoesNotExist", out _));
        }

        [Fact]
        public void GetAll_ReturnsAllPublishedWindows()
        {
            LayoutConflictStore.Clear();
            LayoutConflictStore.Publish(new LayoutReport("A", new List<LayoutConflict>()));
            LayoutConflictStore.Publish(new LayoutReport("B", new List<LayoutConflict>()));
            var all = LayoutConflictStore.GetAll().ToList();
            Assert.Equal(2, all.Count);
            Assert.Contains(all, r => r.WindowName == "A");
            Assert.Contains(all, r => r.WindowName == "B");
        }

        [Fact]
        public void GetWorst_ReturnsHighestSeverity()
        {
            LayoutConflictStore.Clear();
            LayoutConflictStore.Publish(new LayoutReport("Clean", new List<LayoutConflict>()));
            LayoutConflictStore.Publish(new LayoutReport("Dirty", new List<LayoutConflict>
            {
                LayoutConflict.NegativeSize(new LayoutTraceEntry(new Rect(0,0,-1,10), "x", "src"))
            }));
            var worst = LayoutConflictStore.GetWorst();
            Assert.NotNull(worst);
            Assert.Equal("Dirty", worst!.WindowName);
        }

        [Fact]
        public void GetWorst_NoData_ReturnsNull()
        {
            LayoutConflictStore.Clear();
            Assert.Null(LayoutConflictStore.GetWorst());
        }
    }
}
