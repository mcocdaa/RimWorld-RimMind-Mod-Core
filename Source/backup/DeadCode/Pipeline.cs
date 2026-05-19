using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Behaviours
{
    internal sealed class Pipeline<TContext> : IPipeline<TContext>
        where TContext : IPipelineContext
    {
        private readonly IReadOnlyList<IMiddleware<TContext>> _middlewares;

        public Pipeline(IEnumerable<IMiddleware<TContext>> middlewares)
        {
            _middlewares = middlewares
                .OrderBy(m => m.Order)
                .ToList()
                .AsReadOnly();
        }

        public async Task ExecuteAsync(TContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            MiddlewareDelegate<TContext> pipeline = _ =>
            {
                return Task.CompletedTask;
            };

            for (int i = _middlewares.Count - 1; i >= 0; i--)
            {
                var middleware = _middlewares[i];
                var next = pipeline;
                pipeline = ctx =>
                {
                    if (ctx.IsShortCircuited) return Task.CompletedTask;
                    return middleware.InvokeAsync(ctx, next);
                };
            }

            await pipeline(context);
        }
    }
}
