using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Context
{
    internal sealed class CacheStoreMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Name => "ContextCacheStore";
        public int Order => 900;
        public string Id => "ContextCacheStore";

        private readonly IContextCacheManager _cache;
        private readonly ILogSink? _log;

        public CacheStoreMiddleware(IContextCacheManager cache, ILogSink? log = null)
        {
            _cache = cache;
            _log = log;
        }

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            return next(context);
        }
    }
}
