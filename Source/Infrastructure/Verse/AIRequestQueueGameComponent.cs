using Verse;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Queue;
using RimMind.Presentation.Runtime;

namespace RimMind.Infrastructure.Verse
{
    public class AIRequestQueueGameComponent : GameComponent
    {
        private readonly AIRequestQueueImpl _impl;

        public AIRequestQueueGameComponent() : base()
        {
            _impl = RimMindRuntime.Instance?.Queue
                as AIRequestQueueImpl
                ?? throw new System.InvalidOperationException("AIRequestQueueImpl not available.");
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.LogHandler = (msg, isWarning) =>
            {
                if (isWarning) RimMindErrors.Warn(msg);
                else Log.Message(msg);
            };
            _impl.FlushBackgroundQueue = () =>
                RimMindRuntime.Instance?.EventBus.FlushBackgroundQueue();
        }

        public override void GameComponentTick()
        {
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.Tick();
        }

        public override void StartedNewGame() => _impl.Reset();

        public override void LoadedGame() => _impl.Reset();
    }
}
