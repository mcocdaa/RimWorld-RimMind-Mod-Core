using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.Events;

namespace RimMind.Infrastructure.Verse
{
    internal sealed class ContextInvalidationSubscriber
    {
        private readonly IContextCacheManager _cacheManager;
        private readonly ILogSink _logSink;

        public ContextInvalidationSubscriber(IAgentBus eventBus, IContextCacheManager cacheManager, ILogSink logSink)
        {
            _cacheManager = cacheManager;
            _logSink = logSink;
            eventBus.Subscribe<PerceptionEvent>(OnPerception);
        }

        private void OnPerception(PerceptionEvent e)
        {
            _cacheManager.InvalidateNpc(e.NpcId);
            _logSink.Message($"[ContextInvalidation] Invalidated cache for NpcId={e.NpcId} on PerceptionType={e.PerceptionType}");
        }
    }
}
