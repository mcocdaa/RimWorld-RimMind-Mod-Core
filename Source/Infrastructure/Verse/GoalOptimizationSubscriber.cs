using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Domain.Events;

namespace RimMind.Infrastructure.Verse
{
    internal sealed class GoalOptimizationSubscriber
    {
        private readonly ILogSink _logSink;

        public GoalOptimizationSubscriber(IAgentBus eventBus, ILogSink logSink)
        {
            _logSink = logSink;
            eventBus.Subscribe<GoalEvent>(OnGoal);
        }

        private void OnGoal(GoalEvent e)
        {
            _logSink.Message($"[GoalOptimization] Goal changed: NpcId={e.NpcId}, Status={e.Status}, Desc={e.GoalDescription}, Category={e.Category}");
            // Future: trigger StrategyOptimizer to re-evaluate goal priorities.
        }
    }
}
