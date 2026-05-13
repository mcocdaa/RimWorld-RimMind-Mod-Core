using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class CacheLookupMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextCacheLookup";
        public int Order => 100;
        public string Id => "ContextCacheLookup";

        private readonly IContextCacheManager _cache;
        private readonly ILogSink? _log;

        public CacheLookupMiddleware(IContextCacheManager cache, ILogSink? log = null)
        {
            _cache = cache;
            _log = log;
        }

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            string cacheKey = $"ctx:{context.Request.NpcId}:{context.Request.Scenario}";
            if (_cache.TryGetL0CacheItem(cacheKey, out var msg))
            {
                context.CacheHit = true;
                _log?.Message($"[ContextCacheLookup] Cache hit for {cacheKey}");
            }
            return next(context);
        }
    }
}
