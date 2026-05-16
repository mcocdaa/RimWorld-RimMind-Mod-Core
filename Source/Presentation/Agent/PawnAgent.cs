using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.AgentBus;
using RimMind.Domain.Enums;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;
using Verse.AI;

namespace RimMind.Presentation.Agent
{
    public class PawnAgent : IPawnAgent
    {
        public Pawn Pawn { get; }
        private AgentState _state = AgentState.Dormant;
        public AgentState State { get => _state; private set => _state = value; }
        private AgentIdentity _identity = null!;
        public AgentIdentity Identity { get => _identity; private set => _identity = value; }
        private AgentGoalStack _goalStack = new AgentGoalStack();
        public AgentGoalStack GoalStack { get => _goalStack; private set => _goalStack = value; }
        private StrategyOptimizer _strategyOptimizer = new StrategyOptimizer();
        public StrategyOptimizer StrategyOptimizer { get => _strategyOptimizer; private set => _strategyOptimizer = value; }
        public PerceptionBuffer PerceptionBuffer { get; } = new PerceptionBuffer();
        public bool IsActive => State == AgentState.Active;

        private readonly PawnPerceiver _perceiver;
        private readonly PawnThinker _thinker;
        private readonly PawnActor _actor;
        private readonly PawnRecorder _recorder;
        private readonly List<BehaviorRecord> _behaviorHistory = new List<BehaviorRecord>();
#pragma warning disable CS0169
        private Verse.AI.Job? _pendingJob;
#pragma warning restore CS0169
        private int _lastTick;
        private int _tickInterval;
        private int _maxBehaviorHistory;

        IReadOnlyList<BehaviorRecord> IPawnAgent.BehaviorHistory => _behaviorHistory;

        public PawnAgent(Pawn pawn)
        {
            Pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));
            Identity = new AgentIdentity($"NPC-{pawn.thingIDNumber}", pawn.thingIDNumber, pawn.Name?.ToStringFull ?? pawn.Label ?? "Unknown");
            _perceiver = new PawnPerceiver(this);
            _thinker = new PawnThinker(this);
            _actor = new PawnActor(this);
            _recorder = new PawnRecorder(this);
            _tickInterval = RimMindServiceLocator.Get<ISettingsProvider>()?.AgentTickInterval ?? 150;
            _maxBehaviorHistory = RimMindServiceLocator.Get<ISettingsProvider>()?.BehaviorHistoryMax ?? 100;
        }

        public PawnAgent(Pawn pawn, IEventBus eventBus) : this(pawn)
        {
        }

        public void Tick()
        {
            if (State != AgentState.Active) return;
            if (Pawn == null || Pawn.Dead)
            {
                TransitionTo(AgentState.Terminated);
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now - _lastTick < _tickInterval) return;
            _lastTick = now;

            GoalStack.CheckExpired(Pawn.thingIDNumber);
            _perceiver.Tick();
            _thinker.Tick();
            _actor.Tick();
            StrategyOptimizer.DecayAll();
        }

        public bool TransitionTo(AgentState newState)
        {
            if (!AgentStateTransition.CanTransition(State, newState)) return false;
            State = newState;
            return true;
        }

        public void AddGoal(AgentGoal goal)
        {
            GoalStack.TryAdd(goal, Pawn.thingIDNumber);
        }

        public void ForceThink()
        {
            _thinker.ForceThink();
        }

        public Verse.AI.Job? ConsumePendingJob()
        {
            return _actor.ConsumePendingJob();
        }

        public void SetPendingJob(Verse.AI.Job job)
        {
            _actor.SetPendingJob(job);
        }

        public bool RemoveGoal(string goalDescription)
        {
            return GoalStack.Remove(goalDescription, Pawn.thingIDNumber);
        }

        public void RecordBehavior(BehaviorRecord record)
        {
            if (record == null) return;
            _behaviorHistory.Add(record);
            while (_behaviorHistory.Count > _maxBehaviorHistory)
                _behaviorHistory.RemoveAt(0);
            _recorder.Record(record);
        }

        public void Cleanup()
        {
            PerceptionBuffer.Clear();
            _behaviorHistory.Clear();
        }

        public void ResubscribeEvents()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _state, "agentState", AgentState.Dormant);
            Scribe_Deep.Look(ref _identity, "identity");
            Scribe_Deep.Look(ref _goalStack, "goalStack");
            Scribe_Deep.Look(ref _strategyOptimizer, "strategyOptimizer");
        }
    }
}
