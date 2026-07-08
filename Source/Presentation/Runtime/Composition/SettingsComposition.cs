using RimMind.Application.Common.Defaults;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Registry;

namespace RimMind.Presentation.Runtime.Composition
{
    internal static class SettingsComposition
    {
        public static void Register(ISettingsProvider resolvedSettings, IOpenAISettings? openAISettings)
        {
            RimMindServiceLocator.Register(resolvedSettings);

            if (resolvedSettings is IContextSettings contextSettings)
                RimMindServiceLocator.Register(contextSettings);
            if (resolvedSettings is IContextBudgetSettings budgetSettings)
                RimMindServiceLocator.Register(budgetSettings);
            if (resolvedSettings is IContextIncludeSettings includeSettings)
                RimMindServiceLocator.Register(includeSettings);
            if (resolvedSettings is IContextEnvironmentSettings environmentSettings)
                RimMindServiceLocator.Register(environmentSettings);
            if (resolvedSettings is IAIModelSettings aiModelSettings)
                RimMindServiceLocator.Register(aiModelSettings);
            if (resolvedSettings is IApiCredentialSettings apiCredSettings)
                RimMindServiceLocator.Register(apiCredSettings);
            if (resolvedSettings is ICircuitBreakerSettings circuitBreakerSettings)
                RimMindServiceLocator.Register(circuitBreakerSettings);
            if (resolvedSettings is IContextCalibrationSettings calibrationSettings)
                RimMindServiceLocator.Register(calibrationSettings);
            if (resolvedSettings is IQueueSettings queueSettings)
                RimMindServiceLocator.Register(queueSettings);
            if (resolvedSettings is IAgentTickSettings tickSettings)
                RimMindServiceLocator.Register(tickSettings);
            if (resolvedSettings is IDebugSettings debugSettings)
                RimMindServiceLocator.Register(debugSettings);
            if (resolvedSettings is IOverlaySettings overlaySettings)
                RimMindServiceLocator.Register(overlaySettings);
            if (resolvedSettings is IPromptSettings promptSettings)
                RimMindServiceLocator.Register(promptSettings);
            if (resolvedSettings is IFlywheelSettings flywheelSettings)
                RimMindServiceLocator.Register(flywheelSettings);

            if (openAISettings != null)
                RimMindServiceLocator.Register(openAISettings);
            else if (resolvedSettings is IOpenAISettings openAISettingsFromProvider)
                RimMindServiceLocator.Register(openAISettingsFromProvider);
        }

        public static void RegisterApplicationServices(Application.ApplicationServiceBag appBag)
        {
            RimMindServiceLocator.Register(appBag.AgentBus);
            RimMindServiceLocator.Register(appBag.ToolRegistry);
            RimMindServiceLocator.Register(appBag.ParameterStore);
            RimMindServiceLocator.Register(appBag.RuleEngine);
            RimMindServiceLocator.Register(appBag.Queue);
            RimMindServiceLocator.Register<IAIRequestQueueTickable>((IAIRequestQueueTickable)appBag.Queue);
            RimMindServiceLocator.Register(appBag.JsonExtractor);
            RimMindServiceLocator.Register(appBag.Telemetry);
        }

        public static void RegisterInfrastructureServices(RimMind.Infrastructure.InfrastructureServiceBag infraBag)
        {
            RimMindServiceLocator.Register(infraBag.AudioPlayer);
            RimMindServiceLocator.Register(infraBag.TickProvider);
            RimMindServiceLocator.Register(infraBag.ThreadChecker);
            RimMindServiceLocator.Register(infraBag.PathProvider);
            RimMindServiceLocator.Register(infraBag.LogSink);
            RimMindServiceLocator.Register(infraBag.TranslationService);
            RimMindServiceLocator.Register(infraBag.MechanismRegistry);
            RimMindServiceLocator.Register(infraBag.WindowService);
            RimMindServiceLocator.Register(infraBag.AgentActiveChecker);
            RimMindServiceLocator.Register(infraBag.Player2Lifecycle);
            RimMindServiceLocator.Register(infraBag.RequestTraceLog);
        }

        public static void RegisterDefaultExtensionRegistries()
        {
            var modCooldownRegistry = new ExtensionRegistry<IModCooldown>();
            modCooldownRegistry.Register(NullModCooldown.Instance);
            RimMindServiceLocator.Register<IExtensionRegistry<IModCooldown>>(modCooldownRegistry);

            var dialogueTriggerRegistry = new ExtensionRegistry<IDialogueTrigger>();
            dialogueTriggerRegistry.Register(NullDialogueTrigger.Instance);
            RimMindServiceLocator.Register<IExtensionRegistry<IDialogueTrigger>>(dialogueTriggerRegistry);

            var incidentListenerRegistry = new ExtensionRegistry<IIncidentExecutedListener>();
            incidentListenerRegistry.Register(NullIncidentExecutedListener.Instance);
            RimMindServiceLocator.Register<IExtensionRegistry<IIncidentExecutedListener>>(incidentListenerRegistry);

            var skipCheckRegistry = new ExtensionRegistry<ISkipCheck>();
            skipCheckRegistry.Register(NullSkipCheck.Instance);
            RimMindServiceLocator.Register<IExtensionRegistry<ISkipCheck>>(skipCheckRegistry);
        }
    }

    internal static class CompositionRegistry
    {
        public static IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension
        {
            var registry = RimMindServiceLocator.TryGet<IExtensionRegistry<T>>();
            if (registry != null) return registry;

            var newRegistry = new ExtensionRegistry<T>();
            RimMindServiceLocator.Register(newRegistry);
            return newRegistry;
        }
    }
}
