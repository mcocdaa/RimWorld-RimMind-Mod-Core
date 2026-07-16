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
        private string? _registeredLoopKey;

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

            var loopKey = AgentLoopKeys.ForPawn(pawn.thingIDNumber);
            if (_registeredLoopKey != null
                && !string.Equals(_registeredLoopKey, loopKey, System.StringComparison.Ordinal))
            {
                UnregisterFromAgentLoop();
            }

            if (_registeredLoopKey != null)
                return;

            var scheduler = RimMindServiceLocator.TryGet<IAgentLoopScheduler>();
            if (scheduler != null
                && scheduler.Register(loopKey, AgentLoopKind.Pawn, _agent))
            {
                _registeredLoopKey = loopKey;
            }
        }

        private void UnregisterFromAgentLoop()
        {
            if (_registeredLoopKey == null)
                return;

            RimMindServiceLocator.TryGet<IAgentLoopScheduler>()?.Unregister(_registeredLoopKey);
            _registeredLoopKey = null;
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
