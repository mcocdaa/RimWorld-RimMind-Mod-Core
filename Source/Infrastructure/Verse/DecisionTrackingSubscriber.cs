using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Domain.Events;

namespace RimMind.Infrastructure.Verse
{
    /// <summary>
    /// Subscribes to DecisionEvent to record agent decisions for flywheel calibration
    /// and trigger context cache invalidation when decisions change agent state.
    /// </summary>
    internal sealed class DecisionTrackingSubscriber
    {
        private readonly IFlywheelParameterStore _parameterStore;
        private readonly ILogSink _logSink;

        public DecisionTrackingSubscriber(IAgentBus eventBus, IFlywheelParameterStore parameterStore, ILogSink logSink)
        {
            _parameterStore = parameterStore;
            _logSink = logSink;
            eventBus.Subscribe<DecisionEvent>(OnDecision);
        }

        private void OnDecision(DecisionEvent e)
        {
            _logSink.Message($"[DecisionTracking] NpcId={e.NpcId}, Decision={e.DecisionType}, Reason={e.Reason}");
            // Record decision for flywheel calibration: tracks decision patterns
            // to adjust future context budgets and temperature parameters.
            _parameterStore.RecordAction(e.NpcId, e.DecisionType ?? "decision");
        }
    }
}
