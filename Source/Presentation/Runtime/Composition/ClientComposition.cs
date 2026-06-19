using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Diagnostics;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Storage;
using RimMind.Application.Features.Storage;
using RimMind.Domain.Settings;
using RimMind.Domain.Storage;

namespace RimMind.Presentation.Runtime.Composition
{
    internal sealed class ClientCompositionServices
    {
        public IClientManager ClientManager { get; init; } = null!;
        public IExtensionRegistry<IAIClientFactory> ClientFactoryRegistry { get; init; } = null!;
    }

    internal sealed class RemoteSyncCompositionServices
    {
        public RemoteSyncSettings Settings { get; init; } = null!;
        public IRemoteSyncService Service { get; init; } = null!;
    }

    internal static class ClientComposition
    {
        public static ClientCompositionServices RegisterClientManager(ISettingsProvider resolvedSettings)
        {
            var clientFactoryRegistry = CompositionRegistry.GetExtensionRegistry<IAIClientFactory>();
            var clientManager = new ClientManager(resolvedSettings, clientFactoryRegistry);
            RimMindServiceLocator.Register<IClientManager>(clientManager);

            return new ClientCompositionServices
            {
                ClientManager = clientManager,
                ClientFactoryRegistry = clientFactoryRegistry
            };
        }

        public static IAIDebugLog? RegisterBuiltinClientFactories(
            IExtensionRegistry<IAIClientFactory> clientFactoryRegistry,
            ILogSink logSink,
            IOpenAISettings? openAISettings)
        {
            var aiDebugLog = RimMindServiceLocator.TryGet<IAIDebugLog>();
            var resolvedOpenAISettings = openAISettings ?? RimMindServiceLocator.TryGet<IOpenAISettings>();
            Infrastructure.DependencyInjection.RegisterBuiltinClientFactories(
                clientFactoryRegistry,
                logSink,
                aiDebugLog,
                resolvedOpenAISettings);
            RimMindServiceLocator.Register(clientFactoryRegistry);
            AIProviderRegistry.DefaultRegistry = clientFactoryRegistry;
            return aiDebugLog;
        }

        public static RemoteSyncCompositionServices RegisterRemoteSync(ILogSink logSink)
        {
            var remoteSyncSettings = new RemoteSyncSettings();
            RimMindServiceLocator.Register(remoteSyncSettings);

            var remoteBackend = RimMindServiceLocator.TryGet<IRemoteBackend>();
            var remoteSyncOrchestrator = new RemoteSyncOrchestrator(remoteBackend, remoteSyncSettings, logSink);
            var remoteSyncService = new RimMind.Infrastructure.Services.Storage.RemoteSyncService(remoteSyncOrchestrator);
            RimMindServiceLocator.Register<IRemoteSyncService>(remoteSyncService);

            return new RemoteSyncCompositionServices
            {
                Settings = remoteSyncSettings,
                Service = remoteSyncService
            };
        }
    }
}
