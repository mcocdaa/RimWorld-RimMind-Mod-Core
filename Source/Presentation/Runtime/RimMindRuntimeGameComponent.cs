using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Enums;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Presentation.Agent;
using RimMind.Application.Common.Interfaces.Extension;
using Verse;

namespace RimMind.Presentation.Runtime
{
    public class RimMindRuntimeGameComponent : GameComponent
    {
        private readonly Dictionary<int, IPawnAgent> _agents = new Dictionary<int, IPawnAgent>();
        private int _lastTick;
        private bool _initialized;

        public RimMindRuntimeGameComponent(Game game) : base() { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (!_initialized)
            {
                RimMindRuntime.Initialize();
                _initialized = true;
            }

            int now = Find.TickManager.TicksGame;
            if (now == _lastTick) return;
            _lastTick = now;

            foreach (var agent in _agents.Values)
                agent.Tick();
        }

        public IPawnAgent GetOrCreateAgent(Pawn pawn)
        {
            if (pawn == null) throw new System.ArgumentNullException(nameof(pawn));
            if (!_agents.TryGetValue(pawn.thingIDNumber, out var agent))
            {
                agent = new PawnAgent(pawn);
                agent.TransitionTo(AgentState.Active);
                _agents[pawn.thingIDNumber] = agent;
            }
            return agent;
        }

        public IPawnAgent? GetAgent(int pawnId)
        {
            _agents.TryGetValue(pawnId, out var agent);
            return agent;
        }

        public bool RemoveAgent(int pawnId)
        {
            if (_agents.TryGetValue(pawnId, out var agent))
            {
                agent.Cleanup();
                _agents.Remove(pawnId);
                return true;
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            var agentList = new List<PawnAgent>(_agents.Values.Count);
            foreach (var a in _agents.Values)
                if (a is PawnAgent pa)
                    agentList.Add(pa);
            Scribe_Collections.Look(ref agentList, "agents", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _agents.Clear();
                if (agentList != null)
                    foreach (var a in agentList)
                        if (a?.Pawn != null)
                            _agents[a.Pawn.thingIDNumber] = a;
            }
        }
    }
}
