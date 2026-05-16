using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.Enums;
using RimMind.Presentation.Runtime;
using Verse;
using Verse.AI;

namespace RimMind.Presentation.Agent
{
    public class PawnActor
    {
        private readonly IPawnAgent _agent;
        private Verse.AI.Job? _pendingJob;
        private int _lastActionTick;
        private int _actionCooldown = 300;

        public PawnActor(IPawnAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public bool HasPendingJob => _pendingJob != null;

        public void SetPendingJob(Verse.AI.Job job)
        {
            _pendingJob = job;
        }

        public Verse.AI.Job? ConsumePendingJob()
        {
            var job = _pendingJob;
            _pendingJob = null;
            return job;
        }

        public void Tick()
        {
            if (_agent.State != AgentState.Active) return;
            if (_pendingJob != null) return;
            if (Find.TickManager.TicksGame - _lastActionTick < _actionCooldown) return;
        }

        public bool TryExecuteAction(string actionId, string? target = null)
        {
            if (string.IsNullOrEmpty(actionId)) return false;
            _lastActionTick = Find.TickManager.TicksGame;
            return true;
        }
    }
}
