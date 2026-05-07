﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Kernel.Bus;
using RimMind.Core.Settings;
using Verse;

namespace RimMind.Core.Agent
{
    public class PawnRecorder
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly Func<AgentState> _getState;
        private readonly Queue<BehaviorRecord> _behaviorHistory = new Queue<BehaviorRecord>();
        private StrategyOptimizer _strategyOptimizer = new StrategyOptimizer();
        private Action<ActionEvent>? _actionEventHandler;
        private string? _actionSubscriptionKey;

        public IReadOnlyList<BehaviorRecord> BehaviorHistory => _behaviorHistory.ToList();
        public StrategyOptimizer StrategyOptimizer => _strategyOptimizer;

        public PawnRecorder(Pawn pawn, IEventBus eventBus, Func<AgentState> getState)
        {
            _pawn = pawn;
            _eventBus = eventBus;
            _getState = getState;
            _actionEventHandler = OnActionEvent;
            string pawnSubKey = $"PawnAgent_{pawn?.thingIDNumber ?? 0}";
            _actionSubscriptionKey = $"{pawnSubKey}_Action";
            _eventBus.Subscribe<ActionEvent>(_actionSubscriptionKey, _actionEventHandler);
        }

        public void Record(string action, string reason, bool success, string resultReason,
            float goalProgressDelta, int timestamp, string actionEventId)
        {
            _behaviorHistory.Enqueue(new BehaviorRecord
            {
                Action = action,
                Reason = reason,
                Success = success,
                ResultReason = resultReason,
                GoalProgressDelta = goalProgressDelta,
                Timestamp = timestamp,
                ActionEventId = actionEventId,
            });
            while (_behaviorHistory.Count > (RimMindCoreMod.Settings?.behaviorHistoryMax ?? 100))
                _behaviorHistory.Dequeue();
        }

        public void AdjustStrategyWeight(string actionName, float delta)
        {
            _strategyOptimizer.AdjustWeight(actionName, delta);
        }

        private void OnActionEvent(ActionEvent evt)
        {
            if (_getState() != AgentState.Active) return;
            if (_pawn == null) return;
            if (evt.PawnId != _pawn.thingIDNumber) return;

            float delta = evt.Success ? 0.1f : -0.2f;
            _strategyOptimizer.AdjustWeight(evt.ActionName, delta);

            var record = _behaviorHistory.LastOrDefault(r => r.ActionEventId == evt.EventId);
            if (record != null)
            {
                record.Success = evt.Success;
                record.ResultReason = evt.ResultReason ?? "";
            }
        }

        public void Cleanup()
        {
            if (_actionSubscriptionKey != null)
            {
                _eventBus.Unsubscribe<ActionEvent>(_actionSubscriptionKey);
                _actionSubscriptionKey = null;
            }
            _actionEventHandler = null;
        }

        public void Resubscribe()
        {
            if (_actionEventHandler == null)
            {
                _actionEventHandler = OnActionEvent;
                string pawnSubKey = $"PawnAgent_{_pawn?.thingIDNumber ?? 0}";
                _actionSubscriptionKey = $"{pawnSubKey}_Action";
                _eventBus.Subscribe<ActionEvent>(_actionSubscriptionKey, _actionEventHandler);
            }
        }

        public void ExposeData()
        {
            var behaviorHistory = _behaviorHistory.ToList();
            Scribe_Collections.Look(ref behaviorHistory, "behaviorHistory", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _behaviorHistory.Clear();
                if (behaviorHistory != null)
                    foreach (var entry in behaviorHistory)
                        _behaviorHistory.Enqueue(entry);
            }

            var strategyOptimizer = _strategyOptimizer;
            Scribe_Deep.Look(ref strategyOptimizer, "strategyOptimizer");
            if (strategyOptimizer != null) _strategyOptimizer = strategyOptimizer;
        }
    }
}
