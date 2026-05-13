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
        public int Order => 200;
        public string Id => "NpcSnapshotBuild";

        private readonly IContextEngine? _contextEngine;
        private readonly ILogSink? _log;

        public SnapshotBuildMiddleware(IContextEngine? contextEngine = null, ILogSink? log = null)
        {
            _contextEngine = contextEngine;
            _log = log;
        }

        public Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            if (_contextEngine != null)
            {
                _log?.Message($"[SnapshotBuild] Building context snapshot for NPC {context.NpcId}");
            }
            return next(context);
        }
    }
}
