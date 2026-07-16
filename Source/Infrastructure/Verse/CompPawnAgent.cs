using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI;
using RimMind.Presentation.Agent;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class CompProperties_PawnAgent : CompProperties
    {
        public CompProperties_PawnAgent()
        {
            compClass = typeof(CompPawnAgent);
        }
    }

    public class CompPawnAgent : ThingComp
    {
        private IPawnAgentVerse? _agent;
        private IAgentLoopScheduler? _registeredLoopScheduler;
        private string? _registeredLoopKey;
        private int? _registeredPawnId;
        private long? _registeredLoopGeneration;

        public IPawnAgentVerse? Agent
        {
            get => _agent;
            internal set
            {
                if (ReferenceEquals(_agent, value))
                    return;

                UnregisterFromAgentLoop();
                _agent = value;
                EnsureAgentLoopRegistration();
            }
        }

        private IPawnAgentFactoryVerse? _cachedFactory;
        private IAgentBus? _cachedAgentBus;
        private Texture2D? _agentIcon;
        private Texture2D? _agentDevIcon;

        private Pawn Pawn => (Pawn)parent;

        // [Framework-Forced SL] Verse ThingComp requires parameterless constructor.
        // Lazy-cached SL.Get is the only viable pattern; cannot use constructor injection.
        private IPawnAgentFactoryVerse? GetFactory()
            => _cachedFactory ??= RimMindServiceLocator.Get<IPawnAgentFactoryVerse>();

        private IAgentBus? GetAgentBus()
            => _cachedAgentBus ??= RimMindServiceLocator.Get<IAgentBus>();

        public override void CompTick()
        {
            base.CompTick();
            EnsureAgentLoopRegistration();
        }

        private void EnsureAgentLoopRegistration()
        {
            if (_agent == null || _agent.State == AgentState.Terminated)
            {
                UnregisterFromAgentLoop();
                return;
            }

            if (!(parent is Pawn pawn))
            {
                UnregisterFromAgentLoop();
                return;
            }

            var scheduler = RimMindServiceLocator.TryGet<IAgentLoopScheduler>();
            if (scheduler == null)
            {
                UnregisterFromAgentLoop();
                return;
            }

            var schedulerGeneration = scheduler.Generation;
            if (ReferenceEquals(_registeredLoopScheduler, scheduler)
                && _registeredPawnId == pawn.thingIDNumber
                && _registeredLoopGeneration == schedulerGeneration)
            {
                return;
            }

            UnregisterFromAgentLoop();

            var pawnId = pawn.thingIDNumber;
            var loopKey = AgentLoopKeys.ForPawn(pawnId);
            var ownsRegistration = scheduler.Register(loopKey, AgentLoopKind.Pawn, _agent)
                || ReferenceEquals(scheduler.Find(loopKey), _agent);
            if (!ownsRegistration)
                return;

            if (scheduler.Generation != schedulerGeneration)
            {
                scheduler.Unregister(loopKey);
                return;
            }

            _registeredLoopScheduler = scheduler;
            _registeredLoopKey = loopKey;
            _registeredPawnId = pawnId;
            _registeredLoopGeneration = schedulerGeneration;
        }

        private void UnregisterFromAgentLoop()
        {
            var scheduler = _registeredLoopScheduler;
            var loopKey = _registeredLoopKey;
            if (scheduler != null && loopKey != null)
                scheduler.Unregister(loopKey);

            _registeredLoopScheduler = null;
            _registeredLoopKey = null;
            _registeredPawnId = null;
            _registeredLoopGeneration = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            UnregisterFromAgentLoop();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            var factory = GetFactory();
            if (factory != null)
            {
                IPawnAgent? pawnAgent = Agent;
                factory.SerializeAgent(ref pawnAgent, "pawnAgent");
                Agent = pawnAgent as IPawnAgentVerse;
            }

            if (Agent != null && !Agent.IsPawnValid)
            {
                Agent.Destroy();
                Agent = null;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "RimMind.Agent.Gizmo.Control".Translate(),
                defaultDesc = "RimMind.Agent.Gizmo.ControlDesc".Translate(),
                icon = AgentIcon,
                action = () =>
                {
                    Find.WindowStack.Add(Window_RimMindHub.OpenAgentsForPawn(Pawn));
                },
            };

            if (Prefs.DevMode && Agent != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.DevView".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.DevViewDesc".Translate(),
                    icon = AgentDevIcon,
                    action = () =>
                    {
                        Log.Message($"[RimMind-Core] {Pawn.Name?.ToStringShort}\n{Agent.GetDebugInfo()}");
                    },
                };
            }
        }

        private Texture2D AgentIcon =>
            _agentIcon ??= ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false) ?? BaseContent.BadTex;

        private Texture2D AgentDevIcon =>
            _agentDevIcon ??= ContentFinder<Texture2D>.Get("UI/AgentDevIcon", reportFailure: false) ?? BaseContent.BadTex;

        public global::Verse.AI.Job? ConsumePendingJob()
        {
            return Agent?.ConsumePendingJob();
        }

        public static CompPawnAgent? GetComp(Pawn pawn)
        {
            return pawn?.GetComp<CompPawnAgent>();
        }

        public static bool IsAgentActive(Pawn pawn)
        {
            var comp = GetComp(pawn);
            return comp?.Agent?.IsActive == true;
        }

        public bool EnsureAgentCreated()
        {
            if (Agent != null) return true;
            return CreateAgent();
        }

        private bool CreateAgent()
        {
            var factory = GetFactory();
            var agentBus = GetAgentBus();
            if (factory == null || agentBus == null) return false;
            Agent = factory.Create(Pawn, agentBus) as IPawnAgentVerse;
            return Agent != null;
        }

    }
}
