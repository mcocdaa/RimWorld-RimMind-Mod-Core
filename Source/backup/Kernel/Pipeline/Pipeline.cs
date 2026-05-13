using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;

namespace RimMind.Kernel.Pipeline
{
    public sealed class Pipeline<TContext> : IPipeline<TContext> where TContext : IPipelineContext
    {
        private readonly IReadOnlyList<IMiddleware<TContext>> _middlewares;

        public Pipeline(IEnumerable<IMiddleware<TContext>> middlewares)
        {
            _middlewares = middlewares.ToList();
        }

        public async Task ExecuteAsync(TContext context)
        {
            int index = 0;
            async Task Next(TContext ctx)
            {
                if (ctx.IsShortCircuited) return;
                if (index >= _middlewares.Count) return;
                var mw = _middlewares[index++];
                await mw.InvokeAsync(ctx, Next).ConfigureAwait(false);
            }
            await Next(context).ConfigureAwait(false);
        }
    }
}
