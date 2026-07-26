using System;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Presentation.Runtime.Composition;
using RimMind.Presentation.Runtime.Services;
using Verse;

namespace RimMind.Presentation.Runtime
{
    internal static class RimMindRuntimeHost
    {
        private static readonly object Sync = new object();
        private static ExtensionRegistryCatalog _extensions = new ExtensionRegistryCatalog();
        private static readonly AgentActionBridgeSlot ActionBridge = new AgentActionBridgeSlot();
        private static RuntimeComposition? _current;

        public static void Initialize(
            ISettingsProvider? settingsProvider = null,
            IOpenAISettings? openAiSettings = null)
        {
            if (!TryCompose(settingsProvider, openAiSettings, initializeOnly: true, out var error))
                throw new InvalidOperationException("[RimMind-Core] Runtime composition failed.", error);
        }

        public static bool TryRecompose(
            ISettingsProvider? settingsProvider,
            IOpenAISettings? openAiSettings,
            out Exception? error)
            => TryCompose(settingsProvider, openAiSettings, initializeOnly: false, out error);

        private static bool TryCompose(
            ISettingsProvider? settingsProvider,
            IOpenAISettings? openAiSettings,
            bool initializeOnly,
            out Exception? error)
        {
            RuntimeComposition? retiredComposition = null;
            RuntimeLifetime? retiredLifetime = null;
            RuntimeComposition? rejectedCandidate = null;
            long retiredGeneration = 0;
            Guid publishedRuntimeId = Guid.Empty;
            long publishedGeneration = 0;
            int publishedServiceCount = 0;
            var published = false;

            lock (Sync)
            {
                if (initializeOnly && _current != null)
                {
                    error = null;
                    return true;
                }

                RuntimeComposition? candidate = null;
                try
                {
                    var root = new RimMindCompositionRoot();
                    candidate = root.Compose(
                        settingsProvider,
                        openAiSettings,
                        _extensions.Fork(),
                        ActionBridge);
                    var snapshot = candidate.Services.Build();
                    Log.Message($"[RimMind-Core] RuntimeBuildStarted runtimeId={snapshot.RuntimeId}");
                    var publication = RuntimeServiceHub.Shared.Publish(
                        snapshot,
                        candidate.Lifetime,
                        retireReplacedLifetime: false);
                    retiredComposition = _current;
                    retiredLifetime = publication.RetiredLifetime;
                    retiredGeneration = publication.RetiredSnapshot.Generation;
                    _current = candidate;
                    _extensions = candidate.Extensions;
                    publishedRuntimeId = publication.CurrentSnapshot.RuntimeId;
                    publishedGeneration = publication.CurrentSnapshot.Generation;
                    publishedServiceCount = publication.CurrentSnapshot.ServiceCount;
                    published = true;
                    candidate = null;
                    error = null;
                }
                catch (Exception ex)
                {
                    rejectedCandidate = candidate;
                    RuntimeServiceHub.Shared.RecordBuildFailure(ex);
                    error = ex;
                }
            }

            rejectedCandidate?.Dispose();
            if (!published)
            {
                Log.Warning($"[RimMind-Core] RuntimeBuildRejected errorType={error?.GetType().Name}");
                return false;
            }

            Log.Message($"[RimMind-Core] RuntimePublished runtimeId={publishedRuntimeId} generation={publishedGeneration} services={publishedServiceCount}");
            Retire(retiredLifetime, retiredComposition, retiredGeneration);
            return true;
        }

        public static void Shutdown()
        {
            RuntimeComposition? retiredComposition;
            RuntimeLifetime? retiredLifetime;
            long retiredGeneration;

            lock (Sync)
            {
                if (_current == null) return;
                retiredComposition = _current;
                _current = null;
                var publication = RuntimeServiceHub.Shared.Stop(retireReplacedLifetime: false);
                retiredLifetime = publication.RetiredLifetime;
                retiredGeneration = publication.RetiredSnapshot.Generation;
                ActionBridge.Reset();
                _extensions = new ExtensionRegistryCatalog();
            }

            RimMindRuntimeGameComponent.StopGameServices();
            Retire(retiredLifetime, retiredComposition, retiredGeneration);
        }

        private static void Retire(
            RuntimeLifetime? lifetime,
            RuntimeComposition? composition,
            long generation)
        {
            lifetime?.Retire();
            if (composition == null) return;
            var runtimeId = composition.Services.RuntimeId;
            composition.Dispose();
            Log.Message($"[RimMind-Core] RuntimeRetired runtimeId={runtimeId} generation={generation}");
        }
    }
}
