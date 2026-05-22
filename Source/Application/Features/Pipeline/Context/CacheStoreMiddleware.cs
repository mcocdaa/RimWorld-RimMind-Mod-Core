using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class CacheStoreMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextCacheStore";
        public int Order => RimMindDefaults.MiddlewareOrder.CacheStore;
        public string Id => "ContextCacheStore";
        public string OwnerModId => "RimMindCore";

        private readonly IContextCacheManager _cache;
        private readonly ILogSink? _log;

        public CacheStoreMiddleware(IContextCacheManager cache, ILogSink? log = null)
        {
            _cache = cache;
            _log = log;
        }

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            await next(context);

            if (!context.CacheHit && context.Snapshot != null)
            {
                string cacheKey = $"ctx:{context.Request.NpcId}:{context.Request.Scenario}";
                var l0Msg = context.Snapshot.Messages.Count > 0 ? context.Snapshot.Messages[0] : null;
                if (l0Msg != null)
                {
                    _cache.SetL0CacheItem(cacheKey, l0Msg);
                    _log?.Message($"[CacheStore] Stored L0 cache for {cacheKey}");
                }

                if (context.Snapshot.Messages.Count > 1)
                {
                    var l1Blocks = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var msg in context.Snapshot.Messages)
                    {
                        if (msg.LayerTag == "L1" && !string.IsNullOrEmpty(msg.Content))
                        {
                            l1Blocks[msg.LayerTag] = msg.Content;
                        }
                    }
                    if (l1Blocks.Count > 0)
                    {
                        _cache.SetL1BlockCache(context.Request.NpcId, l1Blocks);
                        _log?.Message($"[CacheStore] Stored L1 block cache for NPC {context.Request.NpcId}");
                    }
                }
            }
        }
    }
}
