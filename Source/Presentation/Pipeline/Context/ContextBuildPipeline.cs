using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Presentation.Pipeline.Context
{
    public sealed class ContextBuildPipeline : IPipeline<ContextBuildContext>
    {
        private IReadOnlyList<IMiddleware<ContextBuildContext>> _middlewares = new List<IMiddleware<ContextBuildContext>>();

        public void Use(IMiddleware<ContextBuildContext> middleware)
        {
            var list = new List<IMiddleware<ContextBuildContext>>(_middlewares) { middleware };
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public void UseRange(IEnumerable<IMiddleware<ContextBuildContext>> middlewares)
        {
            var list = new List<IMiddleware<ContextBuildContext>>(_middlewares);
            list.AddRange(middlewares);
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public async Task ExecuteAsync(ContextBuildContext context)
        {
            if (context == null) throw new System.ArgumentNullException(nameof(context));
            int index = 0;
            async Task Next(ContextBuildContext ctx)
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
