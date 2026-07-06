using UnityEngine;
using Xunit;
using RimMind.Infrastructure.UI.AgentsPage;
using RimMind.Infrastructure.UI.Layout;

namespace RimMind.Tests.Infrastructure.UI.Layout
{
    [Collection(LayoutConflictStoreCollection.Name)]
    public class RimMindLayoutScopeTests : System.IDisposable
    {
        public void Dispose()
        {
            LayoutConflictStore.Clear();
        }

        [Fact]
        public void Begin_ReturnsScope_WithRecorder()
        {
            LayoutConflictStore.Clear();
            RimMindLayoutScope scope;
            using (scope = RimMindLayoutScope.Begin("Win", new Rect(0, 0, 100, 100)))
            {
                Assert.Equal(new Rect(0, 0, 100, 100), scope.Recorder.ViewRect);
                Assert.Equal("Win", scope.WindowName);
            }
        }

        [Fact]
        public void Dispose_PublishesReport_ToStore()
        {
            LayoutConflictStore.Clear();
            using (var scope = RimMindLayoutScope.Begin("WinPublish", new Rect(0, 0, 100, 100)))
            {
                scope.Record(new Rect(0, 0, 10, 10), "a");
            }
            Assert.True(LayoutConflictStore.TryGet("WinPublish", out var report));
            Assert.NotNull(report);
            Assert.False(report!.HasConflicts);
        }

        [Fact]
        public void Dispose_PublishesConflicts_WhenDetected()
        {
            LayoutConflictStore.Clear();
            using (var scope = RimMindLayoutScope.Begin("WinConflict", new Rect(0, 0, 100, 100)))
            {
                scope.Record(new Rect(0, 0, 10, 10), "a");
                scope.Record(new Rect(5, 5, 10, 10), "b");
            }
            Assert.True(LayoutConflictStore.TryGet("WinConflict", out var report));
            Assert.True(report!.HasConflicts);
            Assert.Single(report.Conflicts);
        }

        [Fact]
        public void Record_NoLabel_UsesEmptyString()
        {
            LayoutConflictStore.Clear();
            using (var scope = RimMindLayoutScope.Begin("WinNoLabel", new Rect(0, 0, 100, 100)))
            {
                scope.Record(new Rect(0, 0, 10, 10));
            }
            Assert.True(LayoutConflictStore.TryGet("WinNoLabel", out var report));
            Assert.False(report!.HasConflicts);
        }

        [Fact]
        public void LayoutTraceRecorder_DetectsAgentPageSectionOverlap()
        {
            var layout = AgentPageLayout.Calculate(new Rect(0f, 0f, 740f, 480f));
            var recorder = new LayoutTraceRecorder(new Rect(0f, 0f, 740f, 480f));

            recorder.Record(layout.List, "list", "test");
            recorder.Record(layout.Detail, "detail", "test");
            recorder.Record(layout.Status, "status", "test");
            recorder.Record(layout.Actions, "actions", "test");
            recorder.Record(layout.Activity, "activity", "test");
            recorder.Record(layout.Chat, "chat", "test");

            Assert.DoesNotContain(recorder.DetectConflicts(), conflict => conflict.Kind == ConflictKind.Overlap);
        }
    }
}
