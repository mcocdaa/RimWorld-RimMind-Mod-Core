using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Features.Pipeline.Npc;

namespace RimMind.Presentation.Pipeline.Npc
{
    internal sealed class NpcAliveCheckMiddleware : IMiddleware<NpcChatContext>
    {
        private readonly INpcManager? _npcManager;

        public string Id => Name;
        public string OwnerModId => "RimMindCore";
        public string Name => nameof(NpcAliveCheckMiddleware);
        public int Order => 2;

        public NpcAliveCheckMiddleware(INpcManager? npcManager = null)
        {
            _npcManager = npcManager;
        }

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            if (context.Request?.NpcId != null && context.Request.NpcId.StartsWith("NPC-")
                && int.TryParse(context.Request.NpcId.Substring(4), out int pawnId))
            {
                if (_npcManager != null && !_npcManager.IsNpcAlive(context.Request.NpcId))
                {
                    var profile = _npcManager.GetNpc(context.Request.NpcId);
                    if (profile != null)
                        _npcManager.SpawnNpc(profile);
                }
            }
            await next(context);
        }
    }
}
