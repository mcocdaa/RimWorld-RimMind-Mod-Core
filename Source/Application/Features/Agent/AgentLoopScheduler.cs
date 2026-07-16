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
        private int _generation;
        private int _lastTick = -1;
        private int _tickedAgents;
        private int _faultedAgents;
        private bool _isLoopActive;
        private int _activeTick = -1;
        private int? _pendingTick;

        public AgentLoopScheduler(ILogSink? logSink = null)
        {
            _logSink = logSink;
        }

        public int Generation
        {
            get
            {
                lock (_syncRoot)
                {
                    return _generation;
                }
            }
        }

        public bool Register(string key, AgentLoopKind kind, IAgentControl agent)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Agent loop key cannot be blank.", nameof(key));
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));
            if (kind != AgentLoopKind.Pawn && kind != AgentLoopKind.Scoped)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported agent loop kind.");

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
            lock (_syncRoot)
            {
                if (_isLoopActive)
                {
                    if (currentTick <= _activeTick)
                        return;

                    if (!_pendingTick.HasValue || currentTick > _pendingTick.Value)
                        _pendingTick = currentTick;
                    return;
                }

                if (currentTick <= _lastTick)
                    return;

                _isLoopActive = true;
                _activeTick = currentTick;
            }

            var tickToRun = currentTick;
            try
            {
                while (true)
                {
                    List<Entry> tickEntries;
                    lock (_syncRoot)
                    {
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
                        _lastTick = tickToRun;
                        _tickedAgents = tickedAgents;
                        _faultedAgents = faultedAgents;

                        if (_pendingTick.HasValue)
                        {
                            tickToRun = _pendingTick.Value;
                            _pendingTick = null;
                            _activeTick = tickToRun;
                            continue;
                        }

                        _isLoopActive = false;
                        _activeTick = -1;
                        return;
                    }
                }
            }
            catch
            {
                lock (_syncRoot)
                {
                    if (_isLoopActive && _activeTick == tickToRun)
                    {
                        _isLoopActive = false;
                        _activeTick = -1;
                        _pendingTick = null;
                    }
                }

                throw;
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
                _generation++;
            }
        }

        public AgentLoopSnapshot GetSnapshot()
        {
            List<Entry> entries;
            int lastTick;
            int tickedAgents;
            int faultedAgents;
            lock (_syncRoot)
            {
                entries = new List<Entry>(_entries.Values);
                lastTick = _lastTick;
                tickedAgents = _tickedAgents;
                faultedAgents = _faultedAgents;
            }

            var registeredPawnAgents = 0;
            var registeredScopedAgents = 0;
            var activeAgents = 0;
            var pausedAgents = 0;
            var pendingAgents = 0;
            var terminatedAgents = 0;

            foreach (var entry in entries)
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
                lastTick,
                tickedAgents,
                faultedAgents);
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
