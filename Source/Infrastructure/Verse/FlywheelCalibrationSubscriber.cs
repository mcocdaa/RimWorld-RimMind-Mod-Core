using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Flywheel;
using RimMind.Domain.Events;

namespace RimMind.Infrastructure.Verse
{
    internal sealed class FlywheelCalibrationSubscriber
    {
        private readonly IFlywheelParameterStore _parameterStore;
        private readonly ILogSink _logSink;

        public FlywheelCalibrationSubscriber(IAgentBus eventBus, IFlywheelParameterStore parameterStore, ILogSink logSink)
        {
            _parameterStore = parameterStore;
            _logSink = logSink;
            eventBus.Subscribe<ActionEvent>(OnAction);
        }

        private void OnAction(ActionEvent e)
        {
            _parameterStore.RecordAction(e.NpcId, e.ActionName);
            _logSink.Message($"[FlywheelCalibration] Recorded action for NpcId={e.NpcId}, Action={e.ActionName}, Success={e.Success}");
        }
    }
}
