using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Behaviours
{
    public sealed class MutablePipeline<TContext> : IPipeline<TContext>
        where TContext : IPipelineContext
    {
        private IReadOnlyList<IMiddleware<TContext>> _middlewares = new List<IMiddleware<TContext>>();

        public void Use(IMiddleware<TContext> middleware)
        {
            var list = new List<IMiddleware<TContext>>(_middlewares) { middleware };
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public void UseRange(IEnumerable<IMiddleware<TContext>> middlewares)
        {
            var list = new List<IMiddleware<TContext>>(_middlewares);
            list.AddRange(middlewares);
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public async Task ExecuteAsync(TContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
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

    public static class PipelineFactory
    {
        public static IPipeline<TContext> Build<TContext>(
            IReadOnlyList<IMiddleware<TContext>> defaults,
            IExtensionRegistry<IMiddleware<TContext>>? extensions = null)
            where TContext : IPipelineContext
        {
            var extra = extensions?.All ?? Enumerable.Empty<IMiddleware<TContext>>();
            var merged = defaults.Concat(extra).OrderBy(m => m.Order).ToList();
            var pipeline = new MutablePipeline<TContext>();
            pipeline.UseRange(merged);
            return pipeline;
        }
    }
}
