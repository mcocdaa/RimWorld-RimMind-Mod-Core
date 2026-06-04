using System.Collections.Generic;
using System.Linq;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Infrastructure.UI;
using RimMind.Presentation.Agent;
using RimMind.Presentation;
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

        private static Texture2D AgentIcon =>
            ContentFinder<Texture2D>.Get("UI/AgentIcon", reportFailure: false) ?? BaseContent.BadTex;

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
            if (Agent == null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.CreateAgent".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.CreateAgentDesc".Translate(),
                    icon = AgentIcon,
                    action = () =>
                    {
                        var factory = GetFactory();
                        var agentBus = GetAgentBus();
                        if (factory == null || agentBus == null)
                        {
                            Messages.Message(
                                "RimMind.Agent.Gizmo.CreateAgentFailed".Translate(),
                                MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        var createdAgent = factory.Create(Pawn, agentBus);
                        if (createdAgent != null)
                        {
                            Agent = createdAgent;
                            Messages.Message(
                                "RimMind.Agent.Gizmo.AgentCreated".Translate(Pawn.Name?.ToStringShort ?? Pawn.LabelShort),
                                MessageTypeDefOf.PositiveEvent, false);
                        }
                        else
                        {
                            Messages.Message(
                                "RimMind.Agent.Gizmo.CreateAgentFailed".Translate(),
                                MessageTypeDefOf.RejectInput, false);
                        }
                    },
                };

                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.ViewState".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.ViewStateDesc".Translate(),
                    icon = AgentIcon,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_AgentStateDebug(Pawn));
                    },
                };

                yield break;
            }

            string stateLabel = $"RimMind.Agent.State.{Agent.State}".Translate();
            string toggleLabel = Agent.IsActive
                ? "RimMind.Agent.Gizmo.Deactivate".Translate()
                : "RimMind.Agent.Gizmo.Activate".Translate();

            yield return new Command_Action
            {
                defaultLabel = "RimMind.Agent.Gizmo.AgentState".Translate(stateLabel),
                defaultDesc = "RimMind.Agent.Gizmo.ToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/AgentStateIcon", reportFailure: false),
                action = () =>
                {
                    if (Agent.IsActive)
                        Agent.TransitionTo(AgentState.Dormant);
                    else
                        Agent.TransitionTo(AgentState.Active);
                },
            };

            yield return new Command_Action
            {
                defaultLabel = "RimMind.Agent.Gizmo.ViewState".Translate(),
                defaultDesc = "RimMind.Agent.Gizmo.ViewStateDesc".Translate(),
                icon = AgentIcon,
                action = () =>
                {
                    Find.WindowStack.Add(new Window_AgentStateDebug(Pawn));
                },
            };

            if (Agent.IsActive)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.Pause".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.PauseDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentPauseIcon", reportFailure: false),
                    action = () => Agent.TransitionTo(AgentState.Paused),
                };

                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.ForceThink".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.ForceThinkDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentThinkIcon", reportFailure: false),
                    action = () => Agent.ForceThink(),
                };

                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.Dialogue".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.DialogueDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentDialogueIcon", reportFailure: false),
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
                        defaultDesc = "RimMind.Agent.Gizmo.SelectModeDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/AgentModeIcon", reportFailure: false),
                        action = () =>
                        {
                            var options = new List<FloatMenuOption>();
                            foreach (var mode in allModes)
                            {
                                bool isCurrent = mode.ModeId == Agent.CurrentModeId;
                                bool isApplicable = mode.IsApplicable(Agent);

                                string label = isCurrent
                                    ? "RimMind.Agent.Gizmo.CurrentMode".Translate(mode.DisplayName)
                                    : "RimMind.Agent.Gizmo.InactiveMode".Translate(mode.DisplayName);

                                if (!isApplicable && !isCurrent)
                                    label += " (N/A)";

                                options.Add(new FloatMenuOption(label, () =>
                                {
                                    if (!isCurrent && isApplicable)
                                        Agent.SwitchMode(mode.ModeId);
                                    else if (isCurrent)
                                        Messages.Message(
                                            "RimMind.Agent.Gizmo.AlreadyInMode".Translate(mode.DisplayName),
                                            MessageTypeDefOf.RejectInput, false);
                                    else
                                        Messages.Message(
                                            "RimMind.Agent.Gizmo.ModeNotApplicable".Translate(mode.DisplayName),
                                            MessageTypeDefOf.RejectInput, false);
                                })
                                {
                                    Disabled = !isApplicable || isCurrent,
                                });
                            }
                            Find.WindowStack.Add(new FloatMenu(options, "RimMind.Agent.Gizmo.SelectMode".Translate()));
                        },
                    };
                }
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.ForceThink".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.ForceThinkInactiveDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentThinkIcon", reportFailure: false),
                    action = () =>
                    {
                        Messages.Message(
                            "RimMind.Agent.Gizmo.MustBeActive".Translate(),
                            MessageTypeDefOf.RejectInput, false);
                    },
                };
            }

            if (Agent.State == AgentState.Active || Agent.State == AgentState.Paused)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.EmergencyStop".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.EmergencyStopDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentStopIcon", reportFailure: false),
                    action = () =>
                    {
                        if (Agent is IPawnAgent pawnAgent)
                            pawnAgent.PerceptionBuffer.Clear();
                        Agent.TransitionTo(AgentState.Paused);
                    },
                };
            }

            if (Agent.State == AgentState.Paused)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.Resume".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.ResumeDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentResumeIcon", reportFailure: false),
                    action = () => Agent.TransitionTo(AgentState.Active),
                };
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RimMind.Agent.Gizmo.DevView".Translate(),
                    defaultDesc = "RimMind.Agent.Gizmo.DevViewDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AgentDevIcon", reportFailure: false),
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
