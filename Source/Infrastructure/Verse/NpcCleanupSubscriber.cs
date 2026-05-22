using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.Events;

namespace RimMind.Infrastructure.Verse
{
    internal sealed class NpcCleanupSubscriber
    {
        private readonly IContextCacheManager _cacheManager;
        private readonly ILogSink _logSink;

        public NpcCleanupSubscriber(IAgentBus eventBus, IContextCacheManager cacheManager, ILogSink logSink)
        {
            _cacheManager = cacheManager;
            _logSink = logSink;
            eventBus.Subscribe<AgentLifecycleEvent>(OnLifecycle);
        }

        private void OnLifecycle(AgentLifecycleEvent e)
        {
            if (e.NewState == "Dead")
            {
                _cacheManager.InvalidateNpc(e.NpcId);
                _logSink.Message($"[NpcCleanup] Cleaned up cache for dead NPC: NpcId={e.NpcId}");
            }
        }
    }
}
