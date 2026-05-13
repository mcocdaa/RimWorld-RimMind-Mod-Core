using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Presentation.Pipeline.AI
{
    public sealed class AIRequestPipeline : IPipeline<AIRequestContext>
    {
        private IReadOnlyList<IMiddleware<AIRequestContext>> _middlewares = new List<IMiddleware<AIRequestContext>>();

        public void Use(IMiddleware<AIRequestContext> middleware)
        {
            var list = new List<IMiddleware<AIRequestContext>>(_middlewares) { middleware };
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public void UseRange(IEnumerable<IMiddleware<AIRequestContext>> middlewares)
        {
            var list = new List<IMiddleware<AIRequestContext>>(_middlewares);
            list.AddRange(middlewares);
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public async Task ExecuteAsync(AIRequestContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            int index = 0;
            async Task Next(AIRequestContext ctx)
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
