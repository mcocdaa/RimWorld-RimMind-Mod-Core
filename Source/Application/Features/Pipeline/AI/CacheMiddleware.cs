using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.AI
{
    internal sealed class CacheMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "AICache";
        public int Order => RimMindDefaults.MiddlewareOrder.Cache;
        public string Id => "AICache";
        public string OwnerModId => "RimMindCore";

        private readonly ILogSink? _log;

        public CacheMiddleware(ILogSink? log = null) { _log = log; }

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (context.CacheHit)
            {
                _log?.Message($"[AICache] Cache hit for {context.Request.RequestId}");
                context.ShortCircuit("CacheHit");
                return Task.CompletedTask;
            }
            return next(context);
        }
    }
}
