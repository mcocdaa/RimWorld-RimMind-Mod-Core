using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using RimMind.Domain.Events;
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
        public bool IsPawnValid => Pawn != null && !Pawn.Dead;

        private AgentModeId _currentModeId = AgentModeId.Reactive;
        public AgentModeId CurrentModeId => _currentModeId;
        public IAgentMode CurrentMode
            => RimMindAPI.Modes.FindById(_currentModeId.Value)
               ?? throw new InvalidOperationException($"AgentMode '{_currentModeId}' not registered");
        public int? LastThinkTick { get; set; }
        string IAgentInfo.NpcId => Identity.NpcId;
        string IAgentInfo.Label => Pawn?.Label ?? Identity.DisplayName;
        int IAgentInfo.GoalCount => GoalStack.TotalCount;

        private IPawnPerceiver _perceiver;
        private IPawnThinker _thinker;
        private IPawnActor _actor;
        private IPawnRecorder _recorder;
        private readonly List<BehaviorRecord> _behaviorHistory = new List<BehaviorRecord>();
        private readonly IAgentTickSettings? _tickSettings;
        private readonly IAgentBus _agentBus;
        private int _lastTick;
        private int _tickInterval;
        private int _maxBehaviorHistory;

        IReadOnlyList<BehaviorRecord> IPawnAgent.BehaviorHistory => _behaviorHistory;

        public PawnAgent(Pawn pawn, IAgentTickSettings tickSettings, IAgentBus agentBus,
            IPawnPerceiver? perceiver = null, IPawnThinker? thinker = null,
            IPawnActor? actor = null, IPawnRecorder? recorder = null)
        {
            Pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));
            _tickSettings = tickSettings;
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _goalStack.SetAgentBus(_agentBus);
            _perceiver = perceiver!;
            _thinker = thinker!;
            _actor = actor!;
            _recorder = recorder!;
            Identity = new SerializableAgentIdentity($"NPC-{pawn.thingIDNumber}", pawn.thingIDNumber, pawn.Name?.ToStringFull ?? pawn.Label ?? "Unknown");
            _tickInterval = _tickSettings?.AgentTickInterval ?? 150;
            _maxBehaviorHistory = _tickSettings?.BehaviorHistoryMax ?? 100;
        }

        internal void RebuildCollaborators(IPawnPerceiver perceiver, IPawnThinker thinker, IPawnActor actor, IPawnRecorder recorder)
        {
            _perceiver = perceiver ?? throw new ArgumentNullException(nameof(perceiver));
            _thinker = thinker ?? throw new ArgumentNullException(nameof(thinker));
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
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
            var previousState = State;
            State = newState;

            _agentBus?.Publish(new AgentLifecycleEvent(
                Identity.NpcId,
                Pawn?.thingIDNumber ?? -1,
                previousState.ToString(),
                newState.ToString()));

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

        object? IJobProvider.ConsumePendingJob() => ConsumePendingJob();

        public void SetPendingJob(Verse.AI.Job job)
        {
            _actor.SetPendingJob(job);
        }

        public bool RemoveGoal(string goalDescription)
        {
            return GoalStack.Remove(goalDescription, Pawn.thingIDNumber);
        }

        public void RecordBehavior(BehaviorRecordDto dto)
        {
            if (dto == null) return;
            var record = new BehaviorRecord
            {
                Action = dto.Action,
                Reason = dto.Reason,
                Success = dto.Success,
                ResultReason = dto.ResultReason,
                GoalProgressDelta = dto.GoalProgressDelta,
                Timestamp = dto.Timestamp,
                ActionEventId = dto.ActionEventId,
                DurationMs = dto.DurationMs,
            };
            _behaviorHistory.Add(record);
            while (_behaviorHistory.Count > _maxBehaviorHistory)
                _behaviorHistory.RemoveAt(0);
            _recorder.Record(record);
        }

        public void SwitchMode(AgentModeId modeId)
        {
            var newMode = RimMindAPI.Modes.FindById(modeId.Value);
            if (newMode == null)
                throw new InvalidOperationException($"Mode '{modeId}' not registered");
            if (!newMode.IsApplicable(this)) return;
            if (_currentModeId == modeId) return;

            var oldModeId = _currentModeId;
            _currentModeId = modeId;
            LastThinkTick = null;

            var bus = _agentBus;
            if (bus != null)
            {
                bus.Publish(new AgentModeChangedEvent(
                    Identity.NpcId,
                    Pawn?.thingIDNumber ?? -1,
                    oldModeId.Value,
                    modeId.Value));
            }
        }

        public void ResubscribeEvents()
        {
            // After save/load, collaborators are rebuilt by PawnAgentFactory.SerializeAgent,
            // but event subscriptions held by external subscribers may be stale.
            // Re-publish a lifecycle event so subscribers can re-associate this agent.
            _agentBus?.Publish(new AgentLifecycleEvent(
                Identity.NpcId,
                Pawn?.thingIDNumber ?? -1,
                AgentState.Dormant.ToString(),
                State.ToString()));
        }

        public void Cleanup()
        {
            PerceptionBuffer.Clear();
            _behaviorHistory.Clear();
        }

        public void Destroy()
        {
            // Publish final lifecycle event before cleanup
            if (State != AgentState.Terminated)
                TransitionTo(AgentState.Terminated);

            // Clean up resources
            Cleanup();

            // Note: AgentBus uses key-based subscriptions, but PawnAgent doesn't track its subscription keys.
            // The AgentBus.ClearAllSubscribers() in AgentBusGameComponent handles full cleanup on game load.
            // Individual agent unsubscription requires tracking subscription keys (future enhancement).
        }

        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"Goals: {GoalStack.TotalCount}");
            foreach (var g in GoalStack.Goals)
                sb.AppendLine($"  - [{g.Status}] {g.Description} (P:{g.Priority:F1})");
            sb.AppendLine($"Behavior History: {((IPawnAgent)this).BehaviorHistory.Count}");
            var topW = StrategyOptimizer.GetTopN(5);
            if (topW.Count > 0)
            {
                sb.AppendLine("Strategy Weights (Top 5):");
                foreach (var kv in topW)
                    sb.AppendLine($"  {kv.Key}: {kv.Value:F2}");
            }
            return sb.ToString();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _state, "agentState", AgentState.Dormant);
            Scribe_Deep.Look(ref _identity, "identity");
            Scribe_Deep.Look(ref _goalStack, "goalStack");
            Scribe_Deep.Look(ref _strategyOptimizer, "strategyOptimizer");

            string _currentModeIdStr = _currentModeId.Value;
            Scribe_Values.Look(ref _currentModeIdStr, "currentModeId", AgentModeId.Reactive.Value);
            _currentModeId = AgentModeId.Normalize(_currentModeIdStr);
        }

        /// <summary>
        /// Encapsulates Verse serialization type conversion (Scribe_Deep.Look requires concrete type).
        /// Called by PawnAgentFactory.SerializeAgent to keep the cast internal to PawnAgent.
        /// </summary>
        internal static void Serialize(ref IPawnAgent? agent, string label, PawnAgentFactory factory)
        {
            PawnAgent? concrete = agent as PawnAgent;
            Scribe_Deep.Look(ref concrete, label);
            agent = concrete;

            // After deserialization, collaborators are null — rebuild them
            if (Scribe.mode == LoadSaveMode.LoadingVars && concrete != null)
            {
                concrete.RebuildCollaborators(
                    new PawnPerceiver(concrete, factory.AgentBus),
                    new PawnThinker(concrete, factory.TickSettings!, factory.AgentBus),
                    new PawnActor(concrete),
                    new PawnRecorder(concrete, factory.AgentBus));

                // Re-subscribe event bus so subscribers can re-associate this agent after save/load
                concrete.ResubscribeEvents();
            }
        }
    }
}
