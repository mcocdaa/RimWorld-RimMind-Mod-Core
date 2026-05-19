using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Json;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Interfaces.Sensor;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Features.Flywheel;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Queue;
using RimMind.Application.Features.Tools;

namespace RimMind.Application
{
    public static class DependencyInjection
    {
        public static void AddApplicationServices(ISettingsProvider? settingsProvider = null)
        {
            var agentBus = new AgentBusImpl();
            RimMindServiceLocator.Register<IAgentBus>(agentBus);

            var toolRegistry = new ToolRegistry();
            RimMindServiceLocator.Register<IToolRegistry>(toolRegistry);
            RimMindServiceLocator.Register(toolRegistry);

            var parameterStore = new FlywheelParameterStore();
            RimMindServiceLocator.Register<IFlywheelParameterStore>(parameterStore);

            var ruleEngine = new FlywheelRuleEngine(parameterStore);
            RimMindServiceLocator.Register<IFlywheelRuleEngine>(ruleEngine);

            var queue = new AIRequestQueueImpl(() => settingsProvider);
            RimMindServiceLocator.Register<IAIRequestQueue>(queue);
            RimMindServiceLocator.Register<IAIRequestQueueTickable>(queue);
            RimMindServiceLocator.Register(queue);

            var jsonExtractor = new JsonExtractor();
            RimMindServiceLocator.Register<IJsonExtractor>(jsonExtractor);

            var telemetry = new FlywheelTelemetryCollector();
            RimMindServiceLocator.Register<ITelemetryCollector>(telemetry);
            RimMindServiceLocator.Register(telemetry);
        }
    }
}
