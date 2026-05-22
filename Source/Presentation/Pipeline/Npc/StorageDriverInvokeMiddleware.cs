using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Npc;
using RimMind.Application.Features.Pipeline.Npc;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Pipeline.Npc
{
    internal sealed class StorageDriverInvokeMiddleware : IMiddleware<NpcChatContext>
    {
        public string Id => Name;
        public string OwnerModId => "RimMindCore";
        public string Name => nameof(StorageDriverInvokeMiddleware);
        public int Order => 10;

        private readonly IStorageDriverFactory? _storageDriverFactory;
        private readonly ILogSink? _log;

        public StorageDriverInvokeMiddleware(IStorageDriverFactory? storageDriverFactory = null, ILogSink? log = null)
        {
            _storageDriverFactory = storageDriverFactory;
            _log = log;
        }

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            var driver = _storageDriverFactory?.GetDriver();
            if (driver == null)
            {
                _log?.Warning($"[StorageDriverInvoke] No storage driver available for NPC {context.NpcId}");
                context.ChatResult = Result<NpcChatResult, RimMindError>.Err(
                    RimMindErrors.Internal("No storage driver available."));
                await next(context);
                return;
            }

            try
            {
                if (context.Snapshot != null)
                {
                    _log?.Message($"[StorageDriverInvoke] Invoking ChatAsync with snapshot for NPC {context.NpcId}");
                    var result = await driver.ChatAsync(context.Snapshot, context.Ct);
                    context.ChatResult = result;
                }
                else
                {
                    _log?.Message($"[StorageDriverInvoke] Invoking ChatAsync without snapshot for NPC {context.NpcId}");
                    var result = await driver.ChatAsync(context.NpcId, context.Message, context.Context);
                    context.ChatResult = result;
                }
            }
            catch (Exception ex)
            {
                _log?.Warning($"[StorageDriverInvoke] ChatAsync failed for NPC {context.NpcId}: {ex.Message}");
                context.ChatResult = Result<NpcChatResult, RimMindError>.Err(
                    RimMindErrors.Internal(ex.Message, ex));
            }

            await next(context);
        }
    }
}
