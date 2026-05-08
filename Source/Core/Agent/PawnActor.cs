using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Npc;
using RimMind.Core.Perception;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimMind.Core.Agent
{
    public class PawnActor
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly AgentGoalStack _goalStack;
        private readonly PawnRecorder _recorder;
        private PawnDuty? _previousDuty;
        private static readonly ThinkNode_RimMindAgent _pendingJobMarker = new ThinkNode_RimMindAgent();

        public PawnActor(Pawn pawn, IEventBus eventBus, AgentGoalStack goalStack, PawnRecorder recorder)
        {
            _pawn = pawn;
            _eventBus = eventBus;
            _goalStack = goalStack;
            _recorder = recorder;
        }

        public void Execute(string action, string? targetName, string reason)
        {
            string eventId = Guid.NewGuid().ToString("N");

            Pawn? targetPawn = null;
            if (!string.IsNullOrEmpty(targetName) && _pawn?.Map != null)
            {
                if (int.TryParse(targetName, out int targetThingId))
                {
                    var indexed = RimMindServiceLocator.Get<INpcManager>()?.FindPawnByNpcId($"NPC-{targetThingId}");
                    if (indexed != null) targetPawn = indexed as Pawn;
                }
                if (targetPawn == null)
                {
                    targetPawn = _pawn.Map?.mapPawns?.AllPawns?
                        .FirstOrDefault(p => p.thingIDNumber.ToString() == targetName);
                }
                if (targetPawn == null)
                {
                    targetPawn = _pawn.Map?.mapPawns?.AllPawns?
                        .FirstOrDefault(p => p.LabelShortCap == targetName);
                }
            }

            var jobDef = ResolveJobDefForAction(action);
            var job = JobMaker.MakeJob(jobDef);
            if (targetPawn != null)
            {
                job.targetA = new LocalTargetInfo(targetPawn);
            }
            SetPendingJob(job);

            _eventBus.Publish(new DecisionEvent(
                $"NPC-{_pawn?.thingIDNumber}",
                _pawn?.thingIDNumber ?? -1,
                "goal_driven",
                reason,
                action));

            PerceptionBridge.ForwardDecisionAsSignal(action, reason, _pawn?.thingIDNumber ?? -1);

            ApplyDutyForAction(action);
            ApplyDecisionHediffs();
            RecordTaleForDecision(action);

            var topGoal = _goalStack.ActiveCount > 0 ? _goalStack.ActiveGoals[0] : null;
            var progressDelta = ComputeGoalProgressDelta(action, true);

            _recorder.Record(action, reason, true, "Job enqueued", progressDelta,
                Find.TickManager?.TicksGame ?? 0, eventId);

            if (topGoal != null)
            {
                topGoal.Progress += progressDelta;
                if (topGoal.Progress >= 1f)
                {
                    topGoal.Status = GoalStatus.Achieved;
                    _goalStack.Remove(topGoal.Description, _pawn?.thingIDNumber ?? -1);
                }
            }
        }

        public void SetPendingJob(Verse.AI.Job job)
        {
            if (_pawn?.jobs?.jobQueue == null) return;
            job.jobGiver = _pendingJobMarker;
            _pawn.jobs.jobQueue.EnqueueFirst(job, JobTag.Misc);
        }

        public void RestoreOriginalDuty()
        {
            if (_pawn?.mindState == null) return;
            if (_previousDuty != null)
            {
                _pawn.mindState.duty = _previousDuty;
                _previousDuty = null;
            }
        }

        private void ApplyDutyForAction(string action)
        {
            if (_pawn?.mindState == null) return;

            var dutyDef = action switch
            {
                "force_rest" => DefDatabase<DutyDef>.GetNamedSilentFail("RimMind_AgentRest"),
                _ => DefDatabase<DutyDef>.GetNamedSilentFail("RimMind_AgentDecision"),
            };

            if (dutyDef == null) return;

            _previousDuty ??= _pawn.mindState.duty;
            _pawn.mindState.duty = new PawnDuty(dutyDef);
        }

        private void ApplyDecisionHediffs()
        {
            if (_pawn?.health == null) return;

            var focusHediff = _pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("RimMind_AIFocus"));
            if (focusHediff == null)
            {
                var hd = HediffMaker.MakeHediff(HediffDef.Named("RimMind_AIFocus"), _pawn!);
                _pawn!.health!.AddHediff(hd);
            }

            var cooldownHediff = _pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("RimMind_AIDecisionCooldown"));
            if (cooldownHediff == null)
            {
                var hd = HediffMaker.MakeHediff(HediffDef.Named("RimMind_AIDecisionCooldown"), _pawn!);
                _pawn!.health!.AddHediff(hd);
            }
        }

        private void RecordTaleForDecision(string action)
        {
            try
            {
                var taleDef = DefDatabase<TaleDef>.GetNamedSilentFail("RimMind_AgentDecision");
                if (taleDef != null && _pawn != null)
                    TaleRecorder.RecordTale(taleDef, _pawn);
            }
            catch { }
        }

        private static JobDef ResolveJobDefForAction(string action)
        {
            var actionDef = DefDatabase<RimMindActionDef>.AllDefsListForReading
                .FirstOrDefault(d => d.actionId == action);
            if (actionDef?.jobDef != null)
                return actionDef.jobDef;

            string jobDefName = action switch
            {
                "force_rest" or "eat_food" => "RimMind_Rest",
                "assign_work" or "move_to" => "RimMind_Work",
                "socialize" or "chat" => "RimMind_Socialize",
                "tend_pawn" or "rescue_pawn" => "RimMind_EmergencyTend",
                _ => "RimMind_GenericAction"
            };

            var resolved = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
            return resolved ?? DefDatabase<JobDef>.GetNamedSilentFail("RimMind_GenericAction")
                ?? new JobDef { defName = "RimMind_GenericAction" };
        }

        internal static float ComputeGoalProgressDelta(string action, bool executed)
        {
            var actionDef = DefDatabase<RimMindActionDef>.AllDefsListForReading
                .FirstOrDefault(d => d.actionId == action);
            float baseDelta = actionDef?.goalProgressDelta ?? action switch
            {
                "force_rest" => 0.15f,
                "assign_work" => 0.2f,
                "move_to" => 0.05f,
                "tend_pawn" => 0.2f,
                "rescue_pawn" => 0.25f,
                "draft" or "undraft" => 0.1f,
                "eat_food" => 0.15f,
                _ => 0.1f
            };
            return executed ? baseDelta : baseDelta * -0.5f;
        }
    }
}
