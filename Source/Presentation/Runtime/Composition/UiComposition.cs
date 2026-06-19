using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Diagnostics;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Storage;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Settings;
using RimMind.Presentation.Sensor;
using RimMind.Presentation.Settings;
using RimMind.Presentation.UI;

namespace RimMind.Presentation.Runtime.Composition
{
    internal sealed class UiCompositionServices
    {
        public ISensorManager SensorManager { get; init; } = null!;
        public IOverlayService OverlayService { get; init; } = null!;
    }

    internal static class UiComposition
    {
        public static UiCompositionServices RegisterServices()
        {
            var sensorManager = new SensorManager();
            RimMindServiceLocator.Register<ISensorManager>(sensorManager);

            var overlayService = new OverlayService();
            RimMindServiceLocator.Register<IOverlayService>(overlayService);

            RimMindServiceLocator.Register(CompositionRegistry.GetExtensionRegistry<ISettingsTab>());

            return new UiCompositionServices
            {
                SensorManager = sensorManager,
                OverlayService = overlayService
            };
        }

        public static void RegisterRemoteSyncSettingsTab(RemoteSyncSettings settings, IRemoteSyncService service)
        {
            var settingsTabRegistry = RimMindServiceLocator.TryGet<IExtensionRegistry<ISettingsTab>>();
            settingsTabRegistry?.Register(new RemoteSyncSettingsUI(settings, service));
        }

        public static void InitializeDebugActions(
            ISettingsProvider resolvedSettings,
            IAIRequestQueue queue,
            IClientManager clientManager,
            IAIDebugLog? aiDebugLog,
            IContextKeyProvider keyProvider,
            IContextEngine contextEngine,
            IProviderRegistry providerRegistry,
            IContextKeyRegistry contextKeyRegistry,
            IFlywheelParameterStore flywheelParameterStore,
            ITelemetryCollector telemetry,
            IAgentBus agentBus,
            IHistoryManager historyManager,
            INpcManager? npcManager,
            IToolRegistry toolRegistry,
            IGameMechanismRegistry mechanismRegistry)
        {
            RimMind.Infrastructure.UI.RimMindCoreDebugActions.Initialize(
                resolvedSettings,
                queue,
                clientManager,
                aiDebugLog,
                keyProvider,
                contextEngine,
                providerRegistry,
                contextKeyRegistry,
                flywheelParameterStore,
                telemetry,
                agentBus,
                historyManager,
                npcManager,
                toolRegistry,
                mechanismRegistry);
        }
    }
}
