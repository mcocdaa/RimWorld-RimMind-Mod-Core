using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Behaviours
{
    internal sealed class CommonShortCircuitMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        public string Name => "CommonShortCircuit";
        public int Order => int.MinValue + 1;
        public string Id => "CommonShortCircuit";

        public Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            if (context.IsShortCircuited)
                return Task.CompletedTask;

            return next(context);
        }
    }
}
