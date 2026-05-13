using Verse;
using RimMind.Domain.ValueObjects;

namespace RimMind.Infrastructure.Verse
{
    public class AIRequestQueueGameComponent : GameComponent
    {
        private readonly RimMind.Kernel.Queue.AIRequestQueueImpl _impl;

        public AIRequestQueueGameComponent() : base()
        {
            _impl = RimMind.Core.Runtime.RimMindRuntime.Instance?.Queue
                as RimMind.Kernel.Queue.AIRequestQueueImpl
                ?? throw new System.InvalidOperationException("AIRequestQueueImpl not available.");
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.LogHandler = (msg, isWarning) =>
            {
                if (isWarning) RimMindErrors.Warn(msg);
                else Log.Message(msg);
            };
            _impl.FlushBackgroundQueue = () =>
                RimMind.Core.Runtime.RimMindRuntime.Instance?.EventBus.FlushBackgroundQueue();
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
