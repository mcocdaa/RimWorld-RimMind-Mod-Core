using Verse;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Internal;


namespace RimMind.Infrastructure.Verse
{
    public class AIRequestQueueGameComponent : GameComponent
    {
        private readonly IAIRequestQueueTickable _impl;
        private IAgentBus? _cachedAgentBus;

        private IAgentBus? GetAgentBus()
            => _cachedAgentBus ??= RimMindServiceLocator.Get<IAgentBus>();

        public AIRequestQueueGameComponent() : base()
        {
            _impl = RimMindServiceLocator.Get<IAIRequestQueueTickable>()
                ?? throw new System.InvalidOperationException("IAIRequestQueueTickable not available.");
            _impl.CurrentTick = Find.TickManager.TicksGame;
            _impl.LogHandler = (msg, isWarning) =>
            {
                if (isWarning) RimMindErrors.Warn(msg);
                else Log.Message(msg);
            };
            _impl.FlushBackgroundQueue = () =>
                GetAgentBus()?.FlushBackgroundQueue();
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
