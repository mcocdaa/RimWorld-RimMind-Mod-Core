using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Planning;
using RimMind.Application.Common.Interfaces.Agent.Reflection;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Events;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Agent
{
    public class ProactiveBehaviorOrchestrator : IProactiveBehaviorOrchestrator
    {
        private const float TraitConfidenceThreshold = 0.7f;

        private readonly IReflectionStrategy? _reflectionStrategy;
        private readonly IDailyPlanner? _dailyPlanner;
        private readonly IDreamGenerator? _dreamGenerator;
        private readonly IDreamThoughtInjector? _dreamThoughtInjector;
        private readonly ITraitEvolutionEngine? _traitEvolutionEngine;
        private readonly ITraitEvolver? _traitEvolver;
        private readonly IAgentBus _agentBus;
        private readonly int _pawnId;
        private readonly ILogSink? _log;

        public ProactiveBehaviorOrchestrator(
            IReflectionStrategy? reflectionStrategy,
            IDailyPlanner? dailyPlanner,
            IDreamGenerator? dreamGenerator,
            IDreamThoughtInjector? dreamThoughtInjector,
            ITraitEvolutionEngine? traitEvolutionEngine,
            ITraitEvolver? traitEvolver,
            IAgentBus agentBus,
            int pawnId,
            ILogSink? log = null)
        {
            _reflectionStrategy = reflectionStrategy;
            _dailyPlanner = dailyPlanner;
            _dreamGenerator = dreamGenerator;
            _dreamThoughtInjector = dreamThoughtInjector;
            _traitEvolutionEngine = traitEvolutionEngine;
            _traitEvolver = traitEvolver;
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
            _pawnId = pawnId;
            _log = log;
        }

        public void ExecuteReflection(IAgentInfo agent)
        {
            if (_reflectionStrategy?.ShouldReflect(agent) != true) return;
            _reflectionStrategy.ReflectAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _log?.Warning($"[RimMind.Orchestrator] action=ReflectionFailed npcId={agent.NpcId} error={t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.Status == TaskStatus.RanToCompletion && t.Result.IsOk && t.Result.Value.Count > 0)
                    _log?.Message($"[RimMind.Orchestrator] action=ReflectionCompleted npcId={agent.NpcId} insightCount={t.Result.Value.Count}");
            }, TaskScheduler.Current);
        }

        public void ExecutePlanning(IAgentInfo agent)
        {
            if (_dailyPlanner?.ShouldPlan(agent) != true) return;
            _dailyPlanner.PlanAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _log?.Warning($"[RimMind.Orchestrator] action=PlanningFailed npcId={agent.NpcId} error={t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.Status == TaskStatus.RanToCompletion && t.Result.IsOk && t.Result.Value.Count > 0)
                    _log?.Message($"[RimMind.Orchestrator] action=PlanningCompleted npcId={agent.NpcId} blockCount={t.Result.Value.Count}");
            }, TaskScheduler.Current);
        }

        public void ExecuteDream(IAgentInfo agent)
        {
            if (_dreamGenerator?.ShouldDream(agent) != true) return;
            _dreamGenerator.GenerateDreamAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _log?.Warning($"[RimMind.Orchestrator] action=DreamFailed npcId={agent.NpcId} error={t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.Status == TaskStatus.RanToCompletion && t.Result.IsOk)
                {
                    _dreamThoughtInjector?.InjectDreamThought(_pawnId, t.Result.Value);
                    _agentBus.Publish(new DreamEvent(
                        agent.NpcId, _pawnId, t.Result.Value.DreamContent,
                        t.Result.Value.DreamType, t.Result.Value.MoodImpact));
                }
            }, TaskScheduler.Current);
        }

        public void ExecuteTraitEvolution(IAgentInfo agent)
        {
            if (_traitEvolutionEngine?.ShouldEvolve(agent) != true) return;
            var task = _traitEvolutionEngine.EvaluateEvolutionAsync(agent);
            if (task.IsCompleted)
            {
                ProcessTraitEvolutionResult(task, agent);
                return;
            }
            task.ContinueWith(t => ProcessTraitEvolutionResult(t, agent), TaskScheduler.Current);
        }

        private void ProcessTraitEvolutionResult(Task<Result<IReadOnlyList<TraitEvolutionRecord>, RimMindError>> t, IAgentInfo agent)
        {
            if (t.IsFaulted)
            {
                _log?.Warning($"[RimMind.Orchestrator] action=TraitEvolutionFailed npcId={agent.NpcId} error={t.Exception?.InnerException?.Message}");
                return;
            }
            if (t.Status == TaskStatus.RanToCompletion && t.Result.IsOk)
            {
                foreach (var record in t.Result.Value.Where(r => r.Confidence >= TraitConfidenceThreshold))
                {
                    _traitEvolver?.ApplyTraitEvolution(_pawnId, record);
                    _agentBus.Publish(new TraitEvolutionEvent(
                        agent.NpcId, _pawnId, record.TraitDefName,
                        record.Kind, record.Reason, record.Confidence));
                }
            }
        }
    }
}
