using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models.Agent;
using RimMind.Domain.Enums;

namespace RimMind.Application.Features.Agent
{
    public sealed class AgentLoopScheduler : IAgentLoopScheduler
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private readonly ILogSink? _logSink;
        private int _lastTick = -1;
        private int _tickedAgents;
        private int _faultedAgents;

        public AgentLoopScheduler(ILogSink? logSink = null)
        {
            _logSink = logSink;
        }

        public bool Register(string key, AgentLoopKind kind, IAgentControl agent)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Agent loop key cannot be blank.", nameof(key));
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            lock (_syncRoot)
            {
                if (_entries.TryGetValue(key, out var existing)
                    && existing.Kind == kind
                    && ReferenceEquals(existing.Agent, agent))
                {
                    return false;
                }

                _entries[key] = new Entry(key, kind, agent);
                return true;
            }
        }

        public bool Unregister(string key)
        {
            lock (_syncRoot)
            {
                return _entries.Remove(key);
            }
        }

        public IAgentControl? Find(string key)
        {
            lock (_syncRoot)
            {
                return _entries.TryGetValue(key, out var entry) ? entry.Agent : null;
            }
        }

        public void Tick(int currentTick)
        {
            List<Entry> tickEntries;
            lock (_syncRoot)
            {
                if (_lastTick == currentTick)
                    return;

                _lastTick = currentTick;
                tickEntries = new List<Entry>(_entries.Values);
            }

            var tickedAgents = 0;
            var faultedAgents = 0;
            foreach (var entry in tickEntries)
            {
                try
                {
                    entry.Agent.Tick();
                    tickedAgents++;
                }
                catch (Exception ex)
                {
                    faultedAgents++;
                    _logSink?.Error(
                        $"[RimMind.AgentLoop] action=TickFailed key={entry.Key} kind={entry.Kind} error={ex.GetType().Name}: {ex.Message}");
                }
            }

            lock (_syncRoot)
            {
                if (_lastTick == currentTick)
                {
                    _tickedAgents = tickedAgents;
                    _faultedAgents = faultedAgents;
                }
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
            }
        }

        public AgentLoopSnapshot GetSnapshot()
        {
            lock (_syncRoot)
            {
                var registeredPawnAgents = 0;
                var registeredScopedAgents = 0;
                var activeAgents = 0;
                var pausedAgents = 0;
                var pendingAgents = 0;
                var terminatedAgents = 0;

                foreach (var entry in _entries.Values)
                {
                    if (entry.Kind == AgentLoopKind.Pawn)
                        registeredPawnAgents++;
                    else if (entry.Kind == AgentLoopKind.Scoped)
                        registeredScopedAgents++;

                    switch (entry.Agent.State)
                    {
                        case AgentState.Active:
                            activeAgents++;
                            break;
                        case AgentState.Paused:
                            pausedAgents++;
                            break;
                        case AgentState.Terminated:
                            terminatedAgents++;
                            break;
                        default:
                            pendingAgents++;
                            break;
                    }
                }

                return new AgentLoopSnapshot(
                    registeredPawnAgents,
                    registeredScopedAgents,
                    activeAgents,
                    pausedAgents,
                    pendingAgents,
                    terminatedAgents,
                    _lastTick,
                    _tickedAgents,
                    _faultedAgents);
            }
        }

        private sealed class Entry
        {
            public Entry(string key, AgentLoopKind kind, IAgentControl agent)
            {
                Key = key;
                Kind = kind;
                Agent = agent;
            }

            public string Key { get; }
            public AgentLoopKind Kind { get; }
            public IAgentControl Agent { get; }
        }
    }
}
