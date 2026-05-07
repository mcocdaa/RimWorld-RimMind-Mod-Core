using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Core.Comps;
using RimMind.Core.Runtime;
using Verse;
using Verse.AI;

namespace RimMind.Core.Agent
{
    public class JobDriver_RimMindAction : JobDriver
    {
        private string ActionId => job.def.defName == "RimMind_GenericAction" ? (job.GetTarget(TargetIndex.A).Thing as Pawn)?.LabelShortCap ?? "unknown" : job.def.defName;
        private string EventId => job.loadID.ToString("x8");

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var initToil = ToilMaker.MakeToil();
            initToil.initAction = () =>
            {
                var comp = pawn.GetComp<CompPawnAgent>();
                if (comp?.Agent == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var bridge = RimMindRuntime.Instance.GetAgentActionBridge();
                if (bridge == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                var targetPawn = TargetThingA as Pawn;
                string actionId = job.def.defName == "RimMind_GenericAction"
                    ? TargetThingB?.ThingID ?? "unknown"
                    : job.def.defName.Replace("RimMind_", "").ToLowerInvariant();

                bool executed = false;
                try
                {
                    bridge.Execute(pawn, actionId, targetPawn?.LabelShortCap);
                    executed = true;
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[RimMind-Core] JobDriver_RimMindAction bridge error: {ex.Message}");
                }

                if (!executed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                RimMindRuntime.Instance.EventBus.Publish(new DecisionEvent(
                    $"NPC-{pawn.thingIDNumber}",
                    pawn.thingIDNumber,
                    "job_driven",
                    $"JobDriver executed: {actionId}",
                    actionId));
            };
            initToil.defaultCompleteMode = ToilCompleteMode.Instant;
            initToil.atomicWithPrevious = true;
            yield return initToil;

            var waitToil = ToilMaker.MakeToil();
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = 30;
            waitToil.WithEffect(EffecterDefOf.Construction, TargetIndex.None);
            yield return waitToil;

            var finishToil = ToilMaker.MakeToil();
            finishToil.initAction = () =>
            {
                var comp = pawn.GetComp<CompPawnAgent>();
                comp?.Agent?.RecordBehavior(new BehaviorRecord
                {
                    Action = job.def.defName,
                    Reason = "JobDriver completed",
                    Success = true,
                    ResultReason = "Completed via JobDriver",
                    GoalProgressDelta = 0.1f,
                    Timestamp = Find.TickManager.TicksGame,
                    ActionEventId = EventId,
                });
            };
            finishToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finishToil;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            AddFailCondition(() => pawn.Downed || pawn.Dead);
            AddFailCondition(() => TargetThingA != null && TargetThingA.Destroyed);
        }

        protected override void Cleanup(JobCondition condition)
        {
            base.Cleanup(condition);
            if (condition != JobCondition.Succeeded)
            {
                var comp = pawn.GetComp<CompPawnAgent>();
                comp?.Agent?.RecordBehavior(new BehaviorRecord
                {
                    Action = job?.def?.defName ?? "unknown",
                    Reason = "JobDriver failed",
                    Success = false,
                    ResultReason = $"Ended with {condition}",
                    GoalProgressDelta = -0.05f,
                    Timestamp = Find.TickManager.TicksGame,
                    ActionEventId = EventId,
                });
            }
        }
    }
}
