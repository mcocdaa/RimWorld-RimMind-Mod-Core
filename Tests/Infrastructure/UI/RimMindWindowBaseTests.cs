using UnityEngine;
using Xunit;
using Verse;
using RimMind.Infrastructure.UI;
using RimMind.Infrastructure.UI.Layout;
using RimMind.Tests.Infrastructure.UI.Layout;

namespace RimMind.Tests.Infrastructure.UI
{
    // Concrete subclass for testing — RimWorld Window can be constructed in tests
    // because Tests project targets net10.0 with VerseStubs.
    internal sealed class TestRimMindWindow : RimMindWindowBase
    {
        public bool DrawContentsCalled;
        public Rect LastRect;
        public RimMindLayoutScope? LastScope;

        public override Vector2 InitialSize => new Vector2(400f, 300f);

        protected override void DrawContents(Rect inRect, RimMindLayoutScope scope)
        {
            DrawContentsCalled = true;
            LastRect = inRect;
            LastScope = scope;
        }
    }

    [Collection(LayoutConflictStoreCollection.Name)]
    public class RimMindWindowBaseTests : System.IDisposable
    {
        public void Dispose()
        {
            LayoutConflictStore.Clear();
        }

        [Fact]
        public void DrawContents_IsAbstract_OnRimMindWindowBase()
        {
            var method = typeof(RimMindWindowBase).GetMethod(
                "DrawContents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.True(method!.IsAbstract);
        }

        [Fact]
        public void DoWindowContents_CallsDrawContents_WithScope()
        {
            LayoutConflictStore.Clear();
            var w = new TestRimMindWindow();
            w.DoWindowContents(new Rect(0, 0, 400, 300));
            Assert.True(w.DrawContentsCalled);
            Assert.Equal(new Rect(0, 0, 400, 300), w.LastRect);
            Assert.NotNull(w.LastScope);
            Assert.Equal(nameof(TestRimMindWindow), w.LastScope!.WindowName);
        }

        [Fact]
        public void DoWindowContents_PublishesReport_ToStore()
        {
            LayoutConflictStore.Clear();
            var w = new TestRimMindWindow();
            w.DoWindowContents(new Rect(0, 0, 400, 300));
            Assert.True(LayoutConflictStore.TryGet(nameof(TestRimMindWindow), out var report));
            Assert.False(report!.HasConflicts);
        }
    }
}
