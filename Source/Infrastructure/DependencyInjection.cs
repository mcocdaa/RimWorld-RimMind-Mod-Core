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
    /// <summary>
    /// Holds references to all services created by AddInfrastructureServices.
    /// Allows the Composition Root to use direct references instead of resolving back from ServiceLocator.
    /// </summary>
    public sealed class InfrastructureServiceBag
    {
        public IAudioPlayer AudioPlayer { get; init; } = null!;
        public ITickProvider TickProvider { get; init; } = null!;
        public IThreadChecker ThreadChecker { get; init; } = null!;
        public IPathProvider PathProvider { get; init; } = null!;
        public ILogSink LogSink { get; init; } = null!;
        public ITranslationService TranslationService { get; init; } = null!;
        public IGameMechanismRegistry MechanismRegistry { get; init; } = null!;
        public IWindowService WindowService { get; init; } = null!;
        public IAgentActiveChecker AgentActiveChecker { get; init; } = null!;
        public IPlayer2Lifecycle Player2Lifecycle { get; init; } = null!;
        public IStorageDriverFactory StorageDriverFactory { get; init; } = null!;
    }

    public static class DependencyInjection
    {
        public static InfrastructureServiceBag AddInfrastructureServices(
            IToolRegistry toolRegistry, IJsonExtractor jsonExtractor,
            ISettingsProvider? settingsProvider = null)
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

            var mechanismRegistry = new GameMechanismRegistry(toolRegistry, jsonExtractor);
            RimMindServiceLocator.Register<IGameMechanismRegistry>(mechanismRegistry);
            RimMindServiceLocator.Register(mechanismRegistry);

            var windowService = new WindowService();
            RimMindServiceLocator.Register<IWindowService>(windowService);

            var agentActiveChecker = new AgentActiveChecker();
            RimMindServiceLocator.Register<IAgentActiveChecker>(agentActiveChecker);

            var player2Lifecycle = new Player2LifecycleService(settingsProvider);
            RimMindServiceLocator.Register<IPlayer2Lifecycle>(player2Lifecycle);

            var storageDriverFactory = new StorageDriverFactoryService();
            RimMindServiceLocator.Register<IStorageDriverFactory>(storageDriverFactory);

            return new InfrastructureServiceBag
            {
                AudioPlayer = audioPlayer,
                TickProvider = tickProvider,
                ThreadChecker = threadChecker,
                PathProvider = pathProvider,
                LogSink = logSink,
                TranslationService = translationService,
                MechanismRegistry = mechanismRegistry,
                WindowService = windowService,
                AgentActiveChecker = agentActiveChecker,
                Player2Lifecycle = player2Lifecycle,
                StorageDriverFactory = storageDriverFactory
            };
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
