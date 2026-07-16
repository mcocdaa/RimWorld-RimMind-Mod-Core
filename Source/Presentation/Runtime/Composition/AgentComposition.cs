using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Perception;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Features.Agent;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Features.Agent.Psychology;
using RimMind.Application.Features.Agent.Social;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Llm;
using Verse;

namespace RimMind.Presentation.Runtime.Composition
{
    internal sealed class AgentCompositionServices
    {
        public IPawnAgentFactoryVerse PawnAgentFactory { get; init; } = null!;
        public IGameContextBuilder GameContextBuilder { get; init; } = null!;
        public IResponseDispatcher ResponseDispatcher { get; init; } = null!;
    }

    internal static class AgentComposition
    {
        public static AgentCompositionServices RegisterAgents(
            ISettingsProvider resolvedSettings,
            IAgentBus agentBus,
            IActionExecutor actionExecutor,
            InnerVoiceHandler innerVoiceHandler,
            IPsychologyWatcher? psychologyWatcher,
            ITickProvider tickProvider,
            ILogSink logSink,
            INpcManager? npcManager)
        {
            RimMindServiceLocator.Register<IAgentIdentityProvider>(new AgentIdentityProviderAdapter());

            var informationDiffuser = new DefaultInformationDiffuser(agentBus, tickProvider);
            RimMindServiceLocator.Register<IInformationDiffuser>(informationDiffuser);

            var socialEventOrganizer = new DefaultSocialEventOrganizer(tickProvider, agentBus);
            RimMindServiceLocator.Register<ISocialEventOrganizer>(socialEventOrganizer);

            var traitEvolutionEngine = new DefaultTraitEvolutionEngine(tickProvider, psychologyWatcher, agentBus);
            RimMindServiceLocator.Register<ITraitEvolutionEngine>(traitEvolutionEngine);

            var sleepDetector = new RimMind.Infrastructure.Social.VersePawnSleepDetector();
            RimMindServiceLocator.Register<ISleepDetector>(sleepDetector);

            var dreamGenerator = new DefaultDreamGenerator(tickProvider, sleepDetector, agentBus);
            RimMindServiceLocator.Register<IDreamGenerator>(dreamGenerator);

            var traitEvolver = new RimMind.Infrastructure.Social.VerseTraitEvolver();
            RimMindServiceLocator.Register<ITraitEvolver>(traitEvolver);

            var thoughtInjector = RimMindServiceLocator.TryGet<IThoughtInjector>();
            if (thoughtInjector != null)
            {
                var dreamThoughtInjector = new RimMind.Infrastructure.Social.VerseDreamThoughtInjector(thoughtInjector);
                RimMindServiceLocator.Register<IDreamThoughtInjector>(dreamThoughtInjector);
            }

            var agentLoopScheduler = new AgentLoopScheduler(logSink);
            RimMindServiceLocator.Register<IAgentLoopScheduler>(agentLoopScheduler);

            var pawnAgentFactory = new PawnAgentFactory(
                RimMindServiceLocator.Get<IAgentTickSettings>(), agentBus, actionExecutor,
                innerVoiceHandler, psychologyWatcher, tickProvider,
                dreamGenerator, RimMindServiceLocator.TryGet<IDreamThoughtInjector>(), traitEvolver,
                logSink, CompositionRegistry.GetExtensionRegistry<IPerceptionSource>());
            RimMindServiceLocator.Register<IPawnAgentFactoryVerse>(pawnAgentFactory);
            RimMindServiceLocator.Register<IPawnAgentFactory>(pawnAgentFactory);

            var scopedAgentFactory = new ScopedAgentFactory();
            RimMindServiceLocator.Register<IScopedAgentFactory>(scopedAgentFactory);

            var scopedAgentManager = new ScopedAgentManager(scopedAgentFactory, agentLoopScheduler);
            RimMindServiceLocator.Register<IScopedAgentManager>(scopedAgentManager);

            var gameContextBuilder = new GameContextBuilder(
                new PawnContextBuilder(resolvedSettings),
                new MapContextBuilder(resolvedSettings),
                npcManager);
            RimMindServiceLocator.Register<IGameContextBuilder>(gameContextBuilder);

            var responseDispatcher = new ResponseDispatcher(agentBus);
            RimMindServiceLocator.Register<IResponseDispatcher>(responseDispatcher);

            var modePolicyRegistry = CompositionRegistry.GetExtensionRegistry<IModeTransitionPolicy>();
            modePolicyRegistry.Register(new DefaultModeTransitionPolicy());
            RimMindServiceLocator.Register(modePolicyRegistry);

            return new AgentCompositionServices
            {
                PawnAgentFactory = pawnAgentFactory,
                GameContextBuilder = gameContextBuilder,
                ResponseDispatcher = responseDispatcher
            };
        }

        private sealed class AgentIdentityProviderAdapter : IAgentIdentityProvider
        {
            public AgentIdentity? GetAgentIdentity(object pawn)
                => RimMindRuntime.Instance.GetAgentIdentity((Pawn)pawn);
        }
    }
}
