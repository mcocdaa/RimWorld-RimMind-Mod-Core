using System;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using Verse;

namespace RimMind.Presentation.Runtime
{
    public class RimMindRuntimeGameComponent : GameComponent
    {
        private IAgentLoopScheduler? _scheduler;
        private IScopedAgentManager? _scopedAgentManager;
        private IOverlayService? _overlayService;
        private int _lastTick = -1;
        private bool _initialized;

        public RimMindRuntimeGameComponent(Game game) : base() { }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                RimMindRuntime.Initialize();
                _initialized = true;
            }

            _scheduler = RimMindServiceLocator.TryGet<IAgentLoopScheduler>();
            _scopedAgentManager = RimMindServiceLocator.TryGet<IScopedAgentManager>();
            _overlayService = RimMindServiceLocator.TryGet<IOverlayService>();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            EnsureInitialized();
            int now = Find.TickManager.TicksGame;
            if (now == _lastTick) return;
            _lastTick = now;
            _scheduler?.Tick(now);
            _overlayService?.Tick();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            EnsureInitialized();
            ResetRuntimeAgents();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            EnsureInitialized();
            ResetRuntimeAgents();
        }

        private void ResetRuntimeAgents()
        {
            try
            {
                _scopedAgentManager?.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimMind-Core] Failed to clear runtime scoped agents: {ex}");
            }

            try
            {
                _overlayService?.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimMind-Core] Failed to clear pending requests: {ex}");
            }
            finally
            {
                try
                {
                    _scheduler?.Clear();
                }
                finally
                {
                    _lastTick = -1;
                }
            }
        }
    }
}
