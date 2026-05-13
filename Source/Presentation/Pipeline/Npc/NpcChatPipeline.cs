using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Presentation.Pipeline.Npc
{
    public sealed class NpcChatPipeline : IPipeline<NpcChatContext>
    {
        private IReadOnlyList<IMiddleware<NpcChatContext>> _middlewares = new List<IMiddleware<NpcChatContext>>();

        public void Use(IMiddleware<NpcChatContext> middleware)
        {
            var list = new List<IMiddleware<NpcChatContext>>(_middlewares) { middleware };
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public void UseRange(IEnumerable<IMiddleware<NpcChatContext>> middlewares)
        {
            var list = new List<IMiddleware<NpcChatContext>>(_middlewares);
            list.AddRange(middlewares);
            _middlewares = list.OrderBy(m => m.Order).ToList().AsReadOnly();
        }

        public async Task ExecuteAsync(NpcChatContext context)
        {
            if (context == null) throw new System.ArgumentNullException(nameof(context));
            int index = 0;
            async Task Next(NpcChatContext ctx)
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
