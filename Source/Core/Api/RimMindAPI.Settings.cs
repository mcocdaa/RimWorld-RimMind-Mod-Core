using RimMind.Contracts.Context;
using RimMind.Core;
using RimMind.Core.Runtime;
using RimMind.Kernel.Flywheel;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static class Settings
        {
            public static bool IsConfigured() => RimMindCoreMod.Settings.IsConfigured();

            internal static IHistoryManager GetHistoryManager() => RimMindRuntime.Instance.HistoryManager;
            public static IContextEngine GetContextEngine() => RimMindRuntime.Instance.ContextEngine;
            internal static IBudgetScheduler? GetContextScheduler() => RimMindRuntime.Instance.ContextEngine.GetScheduler();
            internal static EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => RimMindRuntime.Instance.ContextEngine.GetEmbeddingSnapshotStore();
            public static FlywheelTelemetryCollector Telemetry => RimMindRuntime.Instance.Telemetry;
        }
    }
}
