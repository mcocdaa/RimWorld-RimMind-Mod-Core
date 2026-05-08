using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Npc;
using RimMind.Core.Settings;
using Verse;
using Verse.AI;

namespace RimMind.Core.Agent
{
    public class PawnAgent : IPawnAgent
    {
        public Pawn Pawn { get; private set; }
        public AgentState State { get; private set; } = AgentState.Dormant;
        public AgentIdentity Identity { get; private set; } = new AgentIdentity();

        private readonly IEventBus _eventBus;
        private readonly AgentGoalStack _goalStack = new AgentGoalStack();
        private readonly PawnPerceiver _perceiver;
        private readonly PawnThinker _thinker;
        private readonly PawnActor _actor;
        private readonly PawnRecorder _recorder;
        private bool _wasInMentalState;

        public AgentGoalStack GoalStack => _goalStack;
        public IReadOnlyList<BehaviorRecord> BehaviorHistory => _recorder.BehaviorHistory;
        public StrategyOptimizer StrategyOptimizer => _recorder.StrategyOptimizer;
        public PerceptionBuffer PerceptionBuffer => _perceiver.Buffer;

        public bool IsActive => State == AgentState.Active;

        public PawnAgent()
        {
            Pawn = null!;
            _eventBus = null!;
            _recorder = new PawnRecorder(null!, null!, () => State);
            _actor = new PawnActor(null!, null!, _goalStack, _recorder);
            _thinker = new PawnThinker(null!, null!, _goalStack, _actor, _recorder);
            _perceiver = new PawnPerceiver(null!, null!, () => State);
        }

        public PawnAgent(Pawn pawn, IEventBus eventBus)
        {
            Pawn = pawn;
            _eventBus = eventBus;
            _recorder = new PawnRecorder(pawn, eventBus, () => State);
            _actor = new PawnActor(pawn, eventBus, _goalStack, _recorder);
            _thinker = new PawnThinker(pawn, eventBus, _goalStack, _actor, _recorder);
            _perceiver = new PawnPerceiver(pawn, eventBus, () => State);
        }

        public void Tick()
        {
            if (State != AgentState.Active) return;
            if (Pawn == null || Pawn.Dead || Pawn.Destroyed) { TransitionTo(AgentState.Terminated); return; }

            if (Pawn.InMentalState)
            {
                _wasInMentalState = true;
                return;
            }

            if (_wasInMentalState)
            {
                _wasInMentalState = false;
                string npcId = $"NPC-{Pawn.thingIDNumber}";
                _eventBus.Publish(new AgentLifecycleEvent(
                    npcId, Pawn.thingIDNumber, "MentalBreak", "MentalStateRecovered"));
            }

            if (!Pawn.IsHashIntervalTick(RimMindCoreMod.Settings?.agentTickInterval ?? 150)) return;

            if (Pawn.IsHashIntervalTick(1500))
            {
                _goalStack.CheckExpired(Pawn?.thingIDNumber ?? -1);
                _recorder.StrategyOptimizer.DecayAll();
            }

            if (Pawn != null && Pawn.jobs?.curJob?.playerForced == true) return;

            var perceptions = _perceiver.Collect();

            if (Find.TickManager.TicksGame - _thinker.LastThinkTick < (RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000)) return;

            if (!CanThinkNow()) return;

            _thinker.Think(perceptions);
            _perceiver.ClearPending();
        }

        private bool CanThinkNow()
        {
            if (Pawn == null) return false;

            if (Pawn.mindState?.duty != null) return false;

            if (Pawn.needs?.food?.CurLevel < 0.3f)
            {
                _recorder.StrategyOptimizer.ApplyNeedUrgencyBoost();
                return true;
            }

            float threatScale = Find.Storyteller?.difficulty?.threatScale ?? 1f;
            if (threatScale > 1.5f)
            {
                int reducedCooldown = (int)((RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000) / threatScale);
                if (Find.TickManager.TicksGame - _thinker.LastThinkTick < reducedCooldown) return false;
            }

            return true;
        }

        public bool TransitionTo(AgentState newState)
        {
            if (!AgentStateTransition.CanTransition(State, newState)) return false;

            var previous = State;
            State = newState;

            int pawnId = Pawn?.thingIDNumber ?? -1;
            if (pawnId >= 0)
            {
                var npcMgr = RimMindServiceLocator.Get<INpcManager>();
                if (newState == AgentState.Active)
                    npcMgr?.RegisterActiveAgent(pawnId);
                else if (previous == AgentState.Active)
                    npcMgr?.UnregisterActiveAgent(pawnId);
            }

            string npcId = Pawn != null ? $"NPC-{Pawn.thingIDNumber}" : "";
            _eventBus.Publish(
                new AgentLifecycleEvent(npcId, pawnId, previous.ToString(), newState.ToString()));

            if (newState == AgentState.Terminated)
                Cleanup();

            return true;
        }

        public void AddGoal(AgentGoal goal)
        {
            if (goal == null) return;
            _goalStack.TryAdd(goal, Pawn?.thingIDNumber ?? -1);
        }

        public void ForceThink() => _thinker.ForceThink();

        public Verse.AI.Job? ConsumePendingJob()
        {
            if (Pawn?.jobs?.jobQueue == null || !Pawn.jobs.jobQueue.AnyCanBeginNow(Pawn, true))
                return null;
            QueuedJob? queued = null;
            var jobQueue = Pawn.jobs.jobQueue;
            for (int i = 0; i < jobQueue.Count; i++)
            {
                var qj = jobQueue[i];
                if (qj?.job?.jobGiver is ThinkNode_RimMindAgent)
                {
                    queued = qj;
                    break;
                }
            }
            if (queued != null)
            {
                var removeMethod = jobQueue.GetType().GetMethod("Remove", new[] { typeof(QueuedJob) });
                if (removeMethod != null)
                    removeMethod.Invoke(jobQueue, new object[] { queued });
                return queued.job;
            }
            return null;
        }

        public void SetPendingJob(Verse.AI.Job job) => _actor.SetPendingJob(job);

        public bool RemoveGoal(string goalDescription)
        {
            return _goalStack.Remove(goalDescription, Pawn?.thingIDNumber ?? -1);
        }

        public void RecordBehavior(BehaviorRecord record)
        {
            _recorder.Record(record.Action, record.Reason, record.Success,
                record.ResultReason, record.GoalProgressDelta, record.Timestamp, record.ActionEventId);
        }

        public void ExposeData()
        {
            var state = State;
            Scribe_Values.Look(ref state, "agentState", AgentState.Dormant);
            State = state;

            var identity = Identity;
            Scribe_Deep.Look(ref identity, "identity");
            Identity = identity ?? new AgentIdentity();

            _goalStack.ExposeData();
            _recorder.ExposeData();
            _thinker.ExposeData();
        }

        public void Cleanup()
        {
            _actor.RestoreOriginalDuty();
            _perceiver.Cleanup();
            _recorder.Cleanup();
            _goalStack.Clear();
        }

        public void ResubscribeEvents()
        {
            _perceiver.Resubscribe();
            _recorder.Resubscribe();
        }

        internal void PublishDecisionAndRecord(string action, string? targetName, string reason)
        {
            _actor.Execute(action, targetName, reason);
        }

        internal static float ComputeGoalProgressDelta(string action, bool executed)
        {
            return PawnActor.ComputeGoalProgressDelta(action, executed);
        }
    }
}
