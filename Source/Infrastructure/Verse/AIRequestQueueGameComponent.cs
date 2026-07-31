using Verse;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Runtime.Services;


namespace RimMind.Infrastructure.Verse
{
    public class AIRequestQueueGameComponent : GameComponent
    {
        private readonly RuntimeServiceRef<IAIRequestQueueTickable> _implRef =
            RuntimeServiceRef<IAIRequestQueueTickable>.Optional();
        private IAIRequestQueueTickable? _impl;

        private void EnsureCached()
        {
            var impl = _implRef.ValueOrDefault;
            if (ReferenceEquals(_impl, impl)) return;
            _impl = impl;
            if (_impl != null)
                Configure(_impl);
        }

        public AIRequestQueueGameComponent() : base() { }

        public AIRequestQueueGameComponent(Game game) : base() { }

        internal static void Configure(IAIRequestQueueTickable impl)
        {
            if (impl == null) throw new System.ArgumentNullException(nameof(impl));
            impl.CurrentTick = Find.TickManager.TicksGame;
            impl.LogHandler = LogQueueMessage;
        }

        private static void LogQueueMessage(string message, bool isWarning)
        {
            if (isWarning) RimMindErrors.Warn(message);
            else Log.Message(message);
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
