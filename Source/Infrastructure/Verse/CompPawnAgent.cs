using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
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
        public IPawnAgentVerse? Agent { get; internal set; }

        private static Texture2D AgentIcon =>
            ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false) ?? BaseContent.BadTex;

        private IPawnAgentFactoryVerse? _cachedFactory;
        private IAgentBus? _cachedAgentBus;

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
            Agent?.Tick();
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
                    icon = ContentFinder<Texture2D>.Get("UI/AgentDevIcon", reportFailure: false) ?? BaseContent.BadTex,
                    action = () =>
                    {
                        Log.Message($"[RimMind-Core] {Pawn.Name?.ToStringShort}\n{Agent.GetDebugInfo()}");
                    },
                };
            }
        }

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
