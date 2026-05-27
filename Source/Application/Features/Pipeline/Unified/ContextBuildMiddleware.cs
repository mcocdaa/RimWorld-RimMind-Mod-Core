using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.Unified
{
    internal sealed class ContextBuildMiddleware : IMiddleware<LlmRequestContext>
    {
        public string Name => "UnifiedContextBuild";
        public int Order => RimMindDefaults.MiddlewareOrder.ContextBuild;
        public string Id => "UnifiedContextBuild";
        public string OwnerModId => "RimMindCore";

        private readonly IContextEngine? _contextEngine;
        private readonly ILogSink? _log;

        public ContextBuildMiddleware(IContextEngine? contextEngine = null, ILogSink? log = null)
        {
            _contextEngine = contextEngine;
            _log = log;
        }

        public async Task InvokeAsync(LlmRequestContext context, MiddlewareDelegate<LlmRequestContext> next)
        {
            // If messages are already populated, skip context building
            if (context.Envelope.Messages != null && context.Envelope.Messages.Count > 0)
            {
                _log?.Message($"[UnifiedContextBuild] Messages already populated ({context.Envelope.Messages.Count}), skipping context build");
                await next(context);
                return;
            }

            // Build context via IContextEngine if available and NPC mode
            if (_contextEngine == null || string.IsNullOrEmpty(context.Envelope.NpcId))
            {
                _log?.Message("[UnifiedContextBuild] No context engine or NpcId, skipping context build");
                await next(context);
                return;
            }

            // Build snapshot directly from envelope fields (no ContextRequest needed)
            var snapshot = await _contextEngine.BuildSnapshotFromEnvelopeAsync(
                context.Envelope.NpcId,
                context.Envelope.GameStateInfo,
                context.Envelope.MaxTokens,
                context.Envelope.Temperature,
                context.Envelope.ScenarioId);

            if (snapshot == null)
            {
                _log?.Warning($"[UnifiedContextBuild] Context build returned null for NPC {context.Envelope.NpcId}");
                context.Result = Result<LlmResponse, RimMindError>.Err(
                    RimMindErrors.ContextBuildFailed($"Context build returned null for NPC {context.Envelope.NpcId}"));
                context.ShortCircuit("context_build_null");
                return;
            }

            context.Snapshot = snapshot;

            // Populate envelope messages from snapshot (both are Domain.Llm.ChatMessage now)
            foreach (var msg in snapshot.Messages)
            {
                context.Envelope.Messages.Add(msg);
            }

            _log?.Message($"[UnifiedContextBuild] Built context for NPC {context.Envelope.NpcId}: {snapshot.Messages.Count} messages, {snapshot.EstimatedTokens} tokens");

            await next(context);
        }

    }
}
