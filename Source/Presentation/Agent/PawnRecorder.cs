using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models;
using RimMind.Domain.Events;
using Verse;

namespace RimMind.Presentation.Agent
{
    public class PawnRecorder : IPawnRecorder
    {
        private readonly IPawnAgent _agent;
        private readonly IAgentBus _agentBus;
        private readonly List<BehaviorRecord> _history = new List<BehaviorRecord>();
        private const int MaxHistory = RimMindDefaults.BehaviorHistoryMax;

        public PawnRecorder(IPawnAgent agent, IAgentBus agentBus)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
        }

        public IReadOnlyList<BehaviorRecord> History => _history;

        public void Record(BehaviorRecord record)
        {
            if (record == null) return;
            _history.Add(record);
            while (_history.Count > MaxHistory)
                _history.RemoveAt(0);
            _agentBus.Publish(new ActionEvent(
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
