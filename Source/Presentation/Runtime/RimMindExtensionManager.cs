using System;
using System.Collections.Concurrent;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Defaults;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Application.Features.Agent.Reflection;
using RimMind.Application.Features.Agent.Planning;
using Verse;

namespace RimMind.Presentation.Runtime
{
    /// <summary>
    /// Extension Manager: responsible for extension registration, middleware, agent identity, and action bridge.
    /// Extracted from RimMindRuntime to satisfy SRP.
    /// </summary>
    internal sealed class RimMindExtensionManager
    {
        private readonly ILogSink? _logSink;
        private readonly ITickProvider? _tickProvider;
        private readonly IAgentBus _agentBus;

        private AgentBusCoreSubscriber? _coreSubscriber;
        private volatile Func<Pawn, AgentIdentity?>? _agentIdentityProvider;
        private volatile IAgentActionBridge _agentActionBridge =
            Application.Common.Defaults.NullAgentActionBridge.Instance;

        public Func<Pawn, AgentIdentity?>? AgentIdentityProvider => _agentIdentityProvider;
        public IAgentActionBridge AgentActionBridge => _agentActionBridge;

        public RimMindExtensionManager(ILogSink? logSink, ITickProvider? tickProvider, IAgentBus agentBus)
        {
            _logSink = logSink;
            _tickProvider = tickProvider;
            _agentBus = agentBus;
        }

        public void RegisterBuiltinModes(IExtensionRegistry<IAgentMode> modeRegistry)
        {
            var tickProvider = _tickProvider
                ?? throw new InvalidOperationException("ITickProvider not registered");
            var reflectionStrategy = new DefaultReflectionStrategy(tickProvider);
            var dailyPlanner = new DefaultDailyPlanner(tickProvider);
            var psychologyWatcher = RimMindServiceLocator.Get<IPsychologyWatcher>();
            var socialEventOrganizer = RimMindServiceLocator.Get<ISocialEventOrganizer>();
            var traitEvolutionEngine = RimMindServiceLocator.Get<ITraitEvolutionEngine>();
            modeRegistry.Register(new ReactiveAgentMode());
            modeRegistry.Register(new ProactiveAgentMode(tickProvider, reflectionStrategy, dailyPlanner, psychologyWatcher, socialEventOrganizer, traitEvolutionEngine));
        }

        public void RegisterCoreSubscribers()
        {
            var logSink = _logSink ?? throw new InvalidOperationException("ILogSink not registered");
            _coreSubscriber = new AgentBusCoreSubscriber(_agentBus, logSink);
        }

        public void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
            => _agentIdentityProvider = provider;

        public AgentIdentity? GetAgentIdentity(Pawn pawn)
            => _agentIdentityProvider?.Invoke(pawn);

        public void RegisterAgentActionBridge(IAgentActionBridge bridge)
        {
            _agentActionBridge = bridge;
            RimMindServiceLocator.Register(bridge);
        }

        public IAgentActionBridge GetAgentActionBridge() => _agentActionBridge;

        public void RegisterParameterTuner(IParameterTuner tuner,
            System.Collections.Concurrent.ConcurrentDictionary<string, IParameterTuner> tuners)
            => tuners[tuner.TunerId] = tuner;

        public void AddMiddleware<TContext>(
            IMiddleware<TContext> middleware,
            IPipeline<BusPublishContext>? busPipeline) where TContext : IPipelineContext
        {
            if (middleware == null) return;
            bool added = false;
            if (busPipeline is MutablePipeline<BusPublishContext> busPipe && middleware is IMiddleware<BusPublishContext> busMw)
            { busPipe.Use(busMw); added = true; }

            if (!added)
            {
                _logSink?.Warning($"[RimMindExtensionManager] AddMiddleware: no pipeline found for TContext={typeof(TContext).Name}, middleware={middleware.Name}");
            }
        }

        public void Reset()
        {
            _agentIdentityProvider = null;
            _agentActionBridge = Application.Common.Defaults.NullAgentActionBridge.Instance;
        }
    }
}
