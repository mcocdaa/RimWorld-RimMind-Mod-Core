using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Behaviours
{
    internal sealed class CommonTraceContextMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        public string Name => "CommonTraceContext";
        public int Order => int.MinValue + 2;
        public string Id => "CommonTraceContext";

        public Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            if (!context.Items.ContainsKey("TraceScope"))
            {
                context.Items["TraceScope"] = TraceContext.BeginScope(context.TraceId);
            }
            return next(context);
        }
    }
}
