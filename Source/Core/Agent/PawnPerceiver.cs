﻿﻿﻿using System;
using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Core.Sensor;
using Verse;

namespace RimMind.Core.Agent
{
    public class PawnPerceiver
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly Func<AgentState> _getState;
        private readonly PerceptionBuffer _perceptionBuffer = new PerceptionBuffer();
        private readonly PerceptionPipeline _perceptionPipeline;
        private readonly List<PerceptionBufferEntry> _pendingPerceptions = new List<PerceptionBufferEntry>();
        private Action<PerceptionEvent>? _perceptionHandler;
        private string? _perceptionSubscriptionKey;

        public PerceptionBuffer Buffer => _perceptionBuffer;

        public PawnPerceiver(Pawn pawn, IEventBus eventBus, Func<AgentState> getState)
        {
            _pawn = pawn;
            _eventBus = eventBus;
            _getState = getState;
            _perceptionPipeline = new PerceptionPipeline();
            _perceptionPipeline.AddFilter(new DedupFilter());
            _perceptionPipeline.AddFilter(new PriorityFilter());
            _perceptionPipeline.AddFilter(new CooldownFilter());
            _perceptionHandler = OnPerceptionEvent;
            string pawnSubKey = $"PawnAgent_{pawn?.thingIDNumber ?? 0}";
            _perceptionSubscriptionKey = $"{pawnSubKey}_Perception";
            _eventBus.Subscribe(_perceptionSubscriptionKey, _perceptionHandler);
        }

        public IReadOnlyList<PerceptionBufferEntry> Collect()
        {
            var raw = _perceptionBuffer.Flush();
            if (raw.Count > 0)
            {
                var filtered = _perceptionPipeline.Process(raw);
                _pendingPerceptions.AddRange(filtered);
            }
            return _pendingPerceptions;
        }

        public void ClearPending() => _pendingPerceptions.Clear();

        public void Cleanup()
        {
            if (_perceptionSubscriptionKey != null)
            {
                _eventBus.Unsubscribe<PerceptionEvent>(_perceptionSubscriptionKey);
                _perceptionSubscriptionKey = null;
            }
            _perceptionHandler = null;
            _perceptionBuffer.Clear();
            _pendingPerceptions.Clear();
        }

        public void Resubscribe()
        {
            if (_perceptionHandler == null)
            {
                _perceptionHandler = OnPerceptionEvent;
                string pawnSubKey = $"PawnAgent_{_pawn?.thingIDNumber ?? 0}";
                _perceptionSubscriptionKey = $"{pawnSubKey}_Perception";
                _eventBus.Subscribe(_perceptionSubscriptionKey, _perceptionHandler);
            }
        }

        private void OnPerceptionEvent(PerceptionEvent evt)
        {
            if (_getState() != AgentState.Active) return;
            if (_pawn == null) return;
            if (evt.PawnId != _pawn.thingIDNumber) return;
            _perceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = evt.PerceptionType,
                Content = evt.Content,
                Importance = evt.Importance,
                Timestamp = evt.Timestamp,
                PawnId = evt.PawnId
            });
        }
    }
}
