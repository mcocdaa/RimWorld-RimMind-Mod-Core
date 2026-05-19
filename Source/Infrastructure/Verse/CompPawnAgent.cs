using System.Collections.Generic;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Interfaces.UI;
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

        private Pawn Pawn => (Pawn)parent;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Agent == null)
            {
                var factory = RimMindServiceLocator.Get<IAgentFactory>();
                if (factory != null)
                    Agent = factory.CreateAgent(Pawn, RimMindServiceLocator.Get<IAgentBus>()!);
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
            var factory = RimMindServiceLocator.Get<IAgentFactory>();
            if (factory != null)
            {
                var agent = Agent;
                factory.SerializeAgent(ref agent, "pawnAgent");
                Agent = agent;
            }

            if (Agent == null && parent is Pawn pawn)
            {
                if (factory != null)
                    Agent = factory.CreateAgent(pawn, RimMindServiceLocator.Get<IAgentBus>()!);
            }

            if (Agent != null && !Agent.IsPawnValid)
            {
                Agent.Cleanup();
                if (factory != null)
                    Agent = factory.CreateAgent(Pawn, RimMindServiceLocator.Get<IAgentBus>()!);
            }

        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Agent == null) yield break;

            string stateLabel = $"RimMind.Presentation.Agent.State.{Agent.State}".Translate();
            string toggleLabel = Agent.IsActive
                ? "RimMind.Presentation.Agent.Gizmo.Deactivate".Translate()
                : "RimMind.Presentation.Agent.Gizmo.Activate".Translate();

            yield return new Command_Action
            {
                defaultLabel = "RimMind.Presentation.Agent.Gizmo.AgentState".Translate(stateLabel),
                defaultDesc = "RimMind.Presentation.Agent.Gizmo.ToggleDesc".Translate(),
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
                    defaultLabel = "RimMind.Presentation.Agent.Gizmo.Dialogue".Translate(),
                    defaultDesc = "RimMind.Presentation.Agent.Gizmo.DialogueDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false),
                    action = () =>
                    {
                        RimMindServiceLocator.Get<IWindowService>()?.OpenAgentDialogue(Pawn);
                    },
                };
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Presentation.Agent.Gizmo.DevView".Translate(),
                    defaultDesc = "RimMind.Presentation.Agent.Gizmo.DevViewDesc".Translate(),
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
            if (Agent is RimMind.Presentation.Agent.IPawnAgent pawnAgent)
                return pawnAgent.ConsumePendingJob();
            return null;
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
