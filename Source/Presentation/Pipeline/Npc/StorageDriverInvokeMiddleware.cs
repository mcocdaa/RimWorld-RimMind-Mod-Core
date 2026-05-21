using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Features.Pipeline.Npc;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Pipeline.Npc
{
    internal sealed class StorageDriverInvokeMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string OwnerModId => "RimMindCore";
        public string Name => nameof(StorageDriverInvokeMiddleware);
        public int Order => 7;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            await next(context);
        }
    }
}
