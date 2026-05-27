using System.Collections.Generic;
using System.Linq;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Agent; // Structural exception: CompPawnAgent (Verse ThingComp) needs IPawnAgentFactory/IPawnAgent
using RimMind.Presentation; // RimMindAPI.Modes for mode switch Gizmo
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

        private IPawnAgentFactory? _cachedFactory;
        private IAgentBus? _cachedAgentBus;
        private IWindowService? _cachedWindowService;

        private Pawn Pawn => (Pawn)parent;

        // [Framework-Forced SL] Verse ThingComp requires parameterless constructor.
        // Lazy-cached SL.Get is the only viable pattern; cannot use constructor injection.
        private IPawnAgentFactory? GetFactory()
            => _cachedFactory ??= RimMindServiceLocator.Get<IPawnAgentFactory>();

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
                    Agent = factory.Create(Pawn, GetAgentBus()!);
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
                IPawnAgent? pawnAgent = Agent as IPawnAgent;
                factory.SerializeAgent(ref pawnAgent, "pawnAgent");
                Agent = pawnAgent;
            }

            if (Agent == null && parent is Pawn pawn)
            {
                if (factory != null)
                    Agent = factory.Create(pawn, GetAgentBus()!);
            }

            if (Agent != null && !Agent.IsPawnValid)
            {
                Agent.Destroy();
                if (factory != null)
                    Agent = factory.Create(Pawn, GetAgentBus()!);
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

                var allModes = RimMindAPI.Modes?.All;
                if (allModes != null && allModes.Count > 1)
                {
                    string currentModeName = Agent.CurrentMode?.DisplayName ?? Agent.CurrentModeId.Value;
                    yield return new Command_Action
                    {
                        defaultLabel = "RimMind.Agent.Gizmo.Mode".Translate(currentModeName),
                        defaultDesc = "RimMind.Agent.Gizmo.ModeDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false),
                        action = () =>
                        {
                            int currentIndex = -1;
                            for (int i = 0; i < allModes.Count; i++)
                            {
                                if (allModes[i].ModeId == Agent.CurrentModeId)
                                {
                                    currentIndex = i;
                                    break;
                                }
                            }

                            // Cycle through modes to find next applicable one
                            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % allModes.Count;
                            for (int attempt = 0; attempt < allModes.Count; attempt++)
                            {
                                var candidate = allModes[nextIndex];
                                if (candidate.IsApplicable(Agent))
                                {
                                    Agent.SwitchMode(candidate.ModeId);
                                    break;
                                }
                                nextIndex = (nextIndex + 1) % allModes.Count;
                            }
                        },
                    };
                }
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
