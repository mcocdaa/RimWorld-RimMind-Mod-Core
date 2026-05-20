using System.Collections.Generic;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Interfaces.Internal;
using RimWorld;
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
        public IAgentControl? Agent { get; internal set; }

        private IAgentFactory? _cachedFactory;
        private IAgentBus? _cachedAgentBus;
        private IWindowService? _cachedWindowService;

        private Pawn Pawn => (Pawn)parent;

        private IAgentFactory? GetFactory()
            => _cachedFactory ??= RimMindServiceLocator.Get<IAgentFactory>();

        private IAgentBus? GetAgentBus()
            => _cachedAgentBus ??= RimMindServiceLocator.Get<IAgentBus>();

        private IWindowService? GetWindowService()
            => _cachedWindowService ??= RimMindServiceLocator.Get<IWindowService>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Agent == null)
            {
                var factory = GetFactory();
                if (factory != null)
                    Agent = factory.CreateAgent(Pawn, GetAgentBus()!);
            }
        }

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
                var agent = Agent;
                factory.SerializeAgent(ref agent, "pawnAgent");
                Agent = agent;
            }

            if (Agent == null && parent is Pawn pawn)
            {
                if (factory != null)
                    Agent = factory.CreateAgent(pawn, GetAgentBus()!);
            }

            if (Agent != null && !Agent.IsPawnValid)
            {
                Agent.Cleanup();
                if (factory != null)
                    Agent = factory.CreateAgent(Pawn, GetAgentBus()!);
            }

        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Agent == null) yield break;

            string stateLabel = $"RimMind.Agent.State.{Agent.State}".Translate();
            string toggleLabel = Agent.IsActive
                ? "RimMind.Agent.Gizmo.Deactivate".Translate()
                : "RimMind.Agent.Gizmo.Activate".Translate();

            yield return new Command_Action
            {
                defaultLabel = "RimMind.Agent.Gizmo.AgentState".Translate(stateLabel),
                defaultDesc = "RimMind.Agent.Gizmo.ToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false),
                action = () =>
                {
                    if (Agent.IsActive)
                        Agent.TransitionTo(AgentState.Dormant);
                    else
                        Agent.TransitionTo(AgentState.Active);
                },
            };

            if (Agent.IsActive)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.Dialogue".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.DialogueDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false),
                    action = () =>
                    {
                        GetWindowService()?.OpenAgentDialogue(Pawn);
                    },
                };
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.DevView".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.DevViewDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false),
                    action = () =>
                    {
                        Log.Message($"[RimMind-Core] {Pawn.Name?.ToStringShort}\n{Agent.GetDebugInfo()}");
                    },
                };
            }
        }

        public global::Verse.AI.Job? ConsumePendingJob()
        {
            return Agent?.ConsumePendingJob() as global::Verse.AI.Job;
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

    }
}
