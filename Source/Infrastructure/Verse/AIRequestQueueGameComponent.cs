using Verse;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Internal;


namespace RimMind.Infrastructure.Verse
{
    public class AIRequestQueueGameComponent : GameComponent
    {
        private IAIRequestQueueTickable? _impl;
        private bool _initialized;

        private void EnsureCached()
        {
            if (_initialized) return;
            var impl = RimMindServiceLocator.TryGet<IAIRequestQueueTickable>();
            if (impl == null) return;
            _impl = impl;
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.LogHandler = (msg, isWarning) =>
            {
                if (isWarning) RimMindErrors.Warn(msg);
                else Log.Message(msg);
            };
            _initialized = true;
        }

        public AIRequestQueueGameComponent() : base() { }

        public AIRequestQueueGameComponent(Game game) : base() { }

        public override void GameComponentTick()
        {
            EnsureCached();
            if (_impl == null) return;
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.Tick();
        }

        public override void StartedNewGame()
        {
            EnsureCached();
            _impl?.Reset();
        }

        public override void LoadedGame()
        {
            EnsureCached();
            _impl?.Reset();
        }
    }
}
