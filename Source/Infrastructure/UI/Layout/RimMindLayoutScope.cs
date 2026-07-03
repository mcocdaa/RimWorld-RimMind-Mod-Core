using UnityEngine;

namespace RimMind.Infrastructure.UI.Layout
{
    /// <summary>
    /// Disposable frame scope wrapping a LayoutTraceRecorder.
    /// On Dispose, runs conflict detection and publishes to LayoutConflictStore.
    /// Usage:
    ///   using (var scope = RimMindLayoutScope.Begin(nameof(MyWindow), inRect))
    ///   {
    ///       scope.Record(someRect, "label");
    ///       ...
    ///   }
    /// </summary>
    public sealed class RimMindLayoutScope : System.IDisposable
    {
        public string WindowName { get; }
        public LayoutTraceRecorder Recorder { get; }
        public bool AutoPublish { get; set; } = true;

        private RimMindLayoutScope(string windowName, Rect viewRect)
        {
            WindowName = windowName;
            Recorder = new LayoutTraceRecorder(viewRect);
        }

        public static RimMindLayoutScope Begin(string windowName, Rect viewRect)
            => new(windowName, viewRect);

        public void Record(Rect rect, string label = "", [System.Runtime.CompilerServices.CallerMemberName] string source = "")
            => Recorder.Record(rect, label, source);

        public void Dispose()
        {
            if (!AutoPublish) return;
            var conflicts = Recorder.DetectConflicts();
            var report = new LayoutReport(WindowName, conflicts);
            LayoutConflictStore.Publish(report);
        }
    }
}
