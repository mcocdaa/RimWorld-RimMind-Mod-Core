using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class ContextBuildTelemetryMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextBuildTelemetry";
        public int Order => int.MinValue;
        public string Id => "ContextBuildTelemetry";
        public string OwnerModId => "RimMindCore";

        private readonly ILogSink? _log;

        public ContextBuildTelemetryMiddleware(ILogSink? log = null) { _log = log; }

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            var start = DateTime.UtcNow;
            try
            {
                await next(context);
            }
            finally
            {
                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                _log?.Message($"[ContextTelemetry] Build for {context.Request.NpcId} took {elapsed:F0}ms" +
                    (context.CacheHit ? " (cache hit)" : ""));
            }
        }
    }
}
