using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Settings
        {
            private static ISettingsProvider? GetSettingsProvider()
                => RimMindRuntime.Instance?.GetSettingsProvider();

            public static bool IsConfigured() => GetSettingsProvider()?.IsConfigured == true;

            public static IContextSettings? ContextSettings => GetSettingsProvider()?.Context;

            public static bool DebugLogging => GetSettingsProvider()?.DebugLogging == true;

            internal static IHistoryManager GetHistoryManager() => RimMindRuntime.Instance.HistoryManager;
            public static IContextEngine GetContextEngine() => RimMindRuntime.Instance.ContextEngine;
            internal static IBudgetScheduler? GetContextScheduler() => RimMindRuntime.Instance.ContextEngine.GetScheduler();
            internal static EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => RimMindRuntime.Instance.ContextEngine.GetEmbeddingSnapshotStore();
            public static ITelemetryCollector Telemetry => RimMindRuntime.Instance.Telemetry;
        }
    }
}
