using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Context
{
    internal sealed class ContextTelemetryEmitter
    {
        private readonly ILogSink? _log;

        public ContextTelemetryEmitter(ILogSink? log = null) { _log = log; }

        public void EmitBuildMetrics(ContextSnapshot snapshot)
        {
            if (snapshot == null || _log == null) return;
            _log.Message($"[ContextTelemetry] Npc={snapshot.NpcId} Scenario={snapshot.Scenario} " +
                $"Tokens={snapshot.EstimatedTokens} Keys={snapshot.IncludedKeys?.Length ?? 0} " +
                $"DiffCount={snapshot.DiffCount}");
        }

        public void EmitCacheHit(string npcId, string key, bool hit)
        {
            _log?.Message($"[ContextTelemetry] CacheHit Npc={npcId} Key={key} Hit={hit}");
        }

        public void EmitLayerLatency(string npcId, string layer, long ms)
        {
            _log?.Message($"[ContextTelemetry] LayerLatency Npc={npcId} Layer={layer} Ms={ms}");
        }
    }
}
