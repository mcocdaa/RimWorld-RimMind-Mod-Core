using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Domain.Events;
using RimMind.Presentation.Runtime;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnRecorder
    {
        private readonly IPawnAgent _agent;
        private readonly List<BehaviorRecord> _history = new List<BehaviorRecord>();
        private const int MaxHistory = 100;

        public PawnRecorder(IPawnAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public IReadOnlyList<BehaviorRecord> History => _history;

        public void Record(BehaviorRecord record)
        {
            if (record == null) return;
            _history.Add(record);
            while (_history.Count > MaxHistory)
                _history.RemoveAt(0);
            RimMindRuntime.Instance.AgentBus.Publish(new ActionEvent(
                _agent.Identity.NpcId, _agent.Pawn?.thingIDNumber ?? 0,
                record.Action, record.Success, record.Reason, Guid.NewGuid().ToString()));
        }

        public void RecordAction(string action, string reason, bool success, float goalProgressDelta = 0f)
        {
            Record(new BehaviorRecord
            {
                Action = action,
                Reason = reason,
                Success = success,
                GoalProgressDelta = goalProgressDelta,
                Timestamp = Find.TickManager.TicksGame
            });
        }

        public void Clear()
        {
            _history.Clear();
        }
    }
}
