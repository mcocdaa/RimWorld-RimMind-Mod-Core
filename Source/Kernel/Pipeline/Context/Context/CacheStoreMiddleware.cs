using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;
using RimMind.Kernel.Context;

namespace RimMind.Core.Pipeline.Context
{
    internal sealed class CacheStoreMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly IContextCacheManager _cacheManager;

        public CacheStoreMiddleware(IContextCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public string Id => Name;
        public string Name => nameof(CacheStoreMiddleware);
        public int Order => 3;

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            if (context.Snapshot != null && context.Snapshot.Messages.Count > 0)
            {
                string l0Key = $"{context.Request.NpcId}_{context.Request.Scenario}";
                var firstMsg = context.Snapshot.Messages[0];
                if (firstMsg != null)
                    _cacheManager.SetL0CacheItem(l0Key, firstMsg);
            }
            return next(context);
        }
    }
}
