using System.Collections.Generic;
using RimMind.Domain.Enums;
using RimMind.Application.Features.AgentBus;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
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

    public class CompPawnAgent : ThingComp, IAgentRecorder
    {
        public IPawnAgent? Agent { get; internal set; }

        private Pawn Pawn => (Pawn)parent;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Agent == null)
            {
                var factory = RimMindServiceLocator.Get<IPawnAgentFactory>();
                if (factory != null)
                    Agent = (IPawnAgent?)factory.Create(Pawn, RimMindServiceLocator.Get<IAgentBus>()!);
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
            var factory = RimMindServiceLocator.Get<IPawnAgentFactory>();
            if (factory != null)
            {
                object? agentObj = Agent;
                factory.SerializeAgent(ref agentObj, "pawnAgent");
                Agent = agentObj as IPawnAgent;
            }

            if (Agent == null && parent is Pawn pawn)
            {
                if (factory != null)
                    Agent = (IPawnAgent?)factory.Create(pawn, RimMindServiceLocator.Get<IAgentBus>()!);
            }

            if (Agent != null && Agent.Pawn == null)
            {
                Agent.Cleanup();
                if (factory != null)
                    Agent = (IPawnAgent?)factory.Create(Pawn, RimMindServiceLocator.Get<IAgentBus>()!);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && Agent != null)
                Agent.ResubscribeEvents();
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
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"State: {Agent.State}");
                        sb.AppendLine($"Goals: {Agent.GoalStack.TotalCount}");
                        foreach (var g in Agent.GoalStack.Goals)
                            sb.AppendLine($"  - [{g.Status}] {g.Description} (P:{g.Priority:F1})");
                        sb.AppendLine($"Behavior History: {Agent.BehaviorHistory.Count}");
                        var topW = Agent.StrategyOptimizer.GetTopN(5);
                        if (topW.Count > 0)
                        {
                            sb.AppendLine("Strategy Weights (Top 5):");
                            foreach (var kv in topW)
                                sb.AppendLine($"  {kv.Key}: {kv.Value:F2}");
                        }
                        Log.Message($"[RimMind-Core] {Pawn.Name?.ToStringShort}\n{sb}");
                    },
                };
            }
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

        public void RecordBehavior(BehaviorRecordDto dto)
        {
            if (dto == null || Agent == null) return;
            Agent.RecordBehavior(dto);
        }
    }
}
