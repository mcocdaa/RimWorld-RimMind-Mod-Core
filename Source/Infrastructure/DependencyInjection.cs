using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Json;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.Persistence;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Infrastructure.Services.Verse;
using RimMind.Infrastructure.UI;
using RimMind.Infrastructure.Verse;
using Verse;

namespace RimMind.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructureServices()
        {
            var audioPlayer = new NullAudioPlayer();
            RimMindServiceLocator.Register<IAudioPlayer>(audioPlayer);

            var tickProvider = new VerseTickProvider();
            RimMindServiceLocator.Register<ITickProvider>(tickProvider);

            var threadChecker = new VerseThreadChecker();
            RimMindServiceLocator.Register<IThreadChecker>(threadChecker);

            var pathProvider = new VersePathProvider();
            RimMindServiceLocator.Register<IPathProvider>(pathProvider);

            var logSink = new VerseLogSink();
            RimMindServiceLocator.Register<ILogSink>(logSink);

            var translationService = new VerseTranslationService();
            RimMindServiceLocator.Register<ITranslationService>(translationService);

            var toolRegistry = RimMindServiceLocator.Get<IToolRegistry>();
            var jsonExtractor = RimMindServiceLocator.Get<IJsonExtractor>();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry, jsonExtractor);
            RimMindServiceLocator.Register<IGameMechanismRegistry>(mechanismRegistry);
            RimMindServiceLocator.Register(mechanismRegistry);

            // UI Services
            RimMindServiceLocator.Register<IWindowService>(new WindowService());

            // Agent Services
            RimMindServiceLocator.Register<IAgentActiveChecker>(new AgentActiveChecker());

            // Player2 Lifecycle
            RimMindServiceLocator.Register<IPlayer2Lifecycle>(new Player2LifecycleService());

            // Storage Driver Factory
            RimMindServiceLocator.Register<IStorageDriverFactory>(new StorageDriverFactoryService());
        }

        public static void AddGameDependentServices()
        {
            // Services that require Current.Game to be available.
            // NpcManager is a GameComponent — Verse instantiates it automatically.
            // Do NOT create a manual instance here; the GameComponent constructor
            // self-registers into RimMindServiceLocator.
            // If Verse hasn't instantiated it yet, downstream code uses null-safe
            // access (?.) and will pick it up once it becomes available.
        }

        public static void RegisterBuiltinClientFactories(IExtensionRegistry<IAIClientFactory> registry,
            ILogSink? logSink = null, IAIDebugLog? aiDebugLog = null, IOpenAISettings? openAISettings = null)
        {
            registry.Register(new OpenAIClientFactory(openAISettings, logSink, aiDebugLog));
            registry.Register(new Player2ClientFactory(logSink, aiDebugLog));
        }
    }
}
