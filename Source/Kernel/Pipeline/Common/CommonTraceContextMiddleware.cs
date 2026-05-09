using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Logging;

namespace RimMind.Core.Pipeline.Common
{
    public sealed class CommonTraceContextMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        public string Id => "Common.TraceContext";
        public string Name => "TraceContext";
        public int Order => -90;

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            using (RimMindLogger.BeginTraceScope(context.TraceId))
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}
