using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Domain.Events;

namespace RimMind.Application.Common.Defaults;

public sealed class AgentBusCoreSubscriber
{
    private readonly ILogSink _logSink;

    public AgentBusCoreSubscriber(IAgentBus eventBus, ILogSink logSink)
    {
        _logSink = logSink;
        eventBus.Subscribe<PerceptionEvent>(OnPerception);
        eventBus.Subscribe<ActionEvent>(OnAction);
        eventBus.Subscribe<AgentModeChangedEvent>(OnModeChanged);
        eventBus.Subscribe<AgentLifecycleEvent>(OnLifecycle);
        eventBus.Subscribe<DecisionEvent>(OnDecision);
        eventBus.Subscribe<GoalEvent>(OnGoal);
    }

    private void OnPerception(PerceptionEvent e)
    {
        _logSink.Message($"[AgentBus] Perception: NpcId={e.NpcId}, PawnId={e.PawnId}, Type={e.PerceptionType}");
    }

    private void OnAction(ActionEvent e)
    {
        _logSink.Message($"[AgentBus] Action: NpcId={e.NpcId}, PawnId={e.PawnId}, Name={e.ActionName}, Success={e.Success}");
    }

    private void OnModeChanged(AgentModeChangedEvent e)
    {
        _logSink.Message($"[AgentBus] ModeChanged: NpcId={e.NpcId}, PawnId={e.PawnId}, {e.OldMode}->{e.NewMode}");
    }

    private void OnLifecycle(AgentLifecycleEvent e)
    {
        _logSink.Message($"[AgentBus] Lifecycle: NpcId={e.NpcId}, PawnId={e.PawnId}, {e.PreviousState}->{e.NewState}");
    }

    private void OnDecision(DecisionEvent e)
    {
        _logSink.Message($"[AgentBus] Decision: NpcId={e.NpcId}, PawnId={e.PawnId}, Type={e.DecisionType}");
    }

    private void OnGoal(GoalEvent e)
    {
        _logSink.Message($"[AgentBus] Goal: NpcId={e.NpcId}, PawnId={e.PawnId}, Status={e.Status}, Desc={e.GoalDescription}");
    }
}
