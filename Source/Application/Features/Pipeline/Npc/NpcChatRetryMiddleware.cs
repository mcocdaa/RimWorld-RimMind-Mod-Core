using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.Npc
{
    internal sealed class NpcChatRetryMiddleware : IMiddleware<NpcChatContext>
    {
        public string Name => "NpcChatRetry";
        public int Order => 800;
        public string Id => "NpcChatRetry";
        public string OwnerModId => "RimMindCore";

        private readonly int _maxRetries;
        private readonly ILogSink? _log;

        public NpcChatRetryMiddleware(int maxRetries = 2, ILogSink? log = null)
        {
            _maxRetries = maxRetries;
            _log = log;
        }

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    await next(context);
                    if (context.Result != null) return;
                }
                catch (Exception ex)
                {
                    context.RetryCount = attempt + 1;
                    _log?.Warning($"[NpcChatRetry] Attempt {attempt + 1} failed: {ex.Message}");
                    if (attempt >= _maxRetries) throw;
                }
            }
        }
    }
}
