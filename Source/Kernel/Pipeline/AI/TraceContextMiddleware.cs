using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.AI;
using RimMind.Kernel.Logging;

namespace RimMind.Core.Pipeline.AI
{
    public sealed class TraceContextMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(TraceContextMiddleware);
        public int Order => 1;

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            using (RimMindLogger.BeginTraceScope(context.TraceId))
            {
                await next(context);
            }
        }
    }
}
