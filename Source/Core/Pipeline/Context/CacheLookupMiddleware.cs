using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Context;

namespace RimMind.Core.Pipeline.Context
{
    internal sealed class CacheLookupMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly IContextCacheManager _cacheManager;

        public CacheLookupMiddleware(IContextCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public string Id => Name;
        public string Name => nameof(CacheLookupMiddleware);
        public int Order => 0;

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            string l0Key = $"{context.Request.NpcId}_{context.Request.Scenario}";
            _cacheManager.TouchCache(l0Key);

            if (_cacheManager.TryGetL1BlockCache(context.Request.NpcId, out var blocks)
                && _cacheManager.TryGetL1Version(context.Request.NpcId, out var version)
                && version > 0)
            {
                context.Items["cache.l1_hit"] = true;
            }

            if (_cacheManager.TryGetL0CacheItem(l0Key, out var msg))
            {
                var snapshot = new ContextSnapshot
                {
                    NpcId = context.Request.NpcId,
                    Scenario = context.Request.Scenario ?? "",
                    MaxTokens = context.Request.MaxTokens,
                    Temperature = context.Request.Temperature,
                };
                snapshot.AddMessage(msg);
                context.Snapshot = snapshot;
                context.ShortCircuit("cache_hit");
                return Task.CompletedTask;
            }

            return next(context);
        }
    }
}
