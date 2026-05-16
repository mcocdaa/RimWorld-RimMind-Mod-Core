using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Features.Tools;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.Verse;
using RimMind.Infrastructure.UI;

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

            var toolRegistry = RimMindServiceLocator.Get<ToolRegistry>();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            RimMindServiceLocator.Register<IGameMechanismRegistry>(mechanismRegistry);
            RimMindServiceLocator.Register(mechanismRegistry);
        }
    }
}
