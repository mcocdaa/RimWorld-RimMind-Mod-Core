using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Npc
{
    internal sealed class SnapshotBuildMiddleware : IMiddleware<NpcChatContext>
    {
        public string Name => "NpcSnapshotBuild";
        public int Order => 5;
        public string Id => "NpcSnapshotBuild";
        public string OwnerModId => "RimMindCore";

        private readonly IContextBuilder? _contextEngine;
        private readonly ILogSink? _log;

        public SnapshotBuildMiddleware(IContextBuilder? contextEngine = null, ILogSink? log = null)
        {
            _contextEngine = contextEngine;
            _log = log;
        }

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            if (context.Snapshot == null && _contextEngine != null)
            {
                _log?.Message($"[SnapshotBuild] Building context snapshot for NPC {context.NpcId}");
                context.Snapshot = _contextEngine.BuildSnapshot(context.Request);
                if (context.Snapshot != null)
                {
                    _log?.Message($"[SnapshotBuild] Snapshot built for NPC {context.NpcId}, estimated {context.Snapshot.EstimatedTokens} tokens");
                }
            }
            await next(context);
        }
    }
}
