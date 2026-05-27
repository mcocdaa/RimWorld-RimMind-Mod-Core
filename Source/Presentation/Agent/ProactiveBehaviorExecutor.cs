using System;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Agent.Modes;
using RimMind.Domain.Events;
using RimMind.Infrastructure.Social;
using Verse;

namespace RimMind.Presentation.Agent
{
    /// <summary>
    /// Executes proactive-mode extension behaviors: Reflection, Planning, Dream, TraitEvolution.
    /// Extracted from PawnThinker to keep PawnThinker focused on mode coordination.
    /// </summary>
    internal sealed class ProactiveBehaviorExecutor
    {
        private readonly IAgentBus _agentBus;
        private IDreamGenerator? _dreamGenerator;
        private VerseDreamThoughtInjector? _dreamThoughtInjector;
        private VerseTraitEvolver? _traitEvolver;

        private IDreamGenerator? GetDreamGenerator()
            => _dreamGenerator ??= RimMindServiceLocator.Get<IDreamGenerator>();

        private VerseDreamThoughtInjector? GetDreamThoughtInjector()
            => _dreamThoughtInjector ??= RimMindServiceLocator.Get<VerseDreamThoughtInjector>();

        private VerseTraitEvolver? GetTraitEvolver()
            => _traitEvolver ??= RimMindServiceLocator.Get<VerseTraitEvolver>();

        public ProactiveBehaviorExecutor(IAgentBus agentBus)
        {
            _agentBus = agentBus ?? throw new ArgumentNullException(nameof(agentBus));
        }

        public void ExecuteProactiveExtensions(IPawnAgent agent, IAgentMode mode, int pawnId)
        {
            if (mode is not ProactiveAgentMode proactive) return;

            ExecuteReflection(proactive, agent);
            ExecutePlanning(proactive, agent);
            ExecuteDream(agent, pawnId);
            ExecuteTraitEvolution(proactive, agent, pawnId);
        }

        private void ExecuteReflection(ProactiveAgentMode proactive, IPawnAgent agent)
        {
            if (proactive.ReflectionStrategy?.ShouldReflect(agent) != true) return;
            proactive.ReflectionStrategy.ReflectAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Warning($"[Think] Reflection failed for {agent.Identity.NpcId}: {t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.IsCompletedSuccessfully && t.Result.IsOk && t.Result.Value.Count > 0)
                    Log.Message($"[RimMind] Reflection: {agent.Identity.NpcId} generated {t.Result.Value.Count} insights");
            }, TaskScheduler.Current);
        }

        private void ExecutePlanning(ProactiveAgentMode proactive, IPawnAgent agent)
        {
            if (proactive.DailyPlanner?.ShouldPlan(agent) != true) return;
            proactive.DailyPlanner.PlanAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Warning($"[Think] Planning failed for {agent.Identity.NpcId}: {t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.IsCompletedSuccessfully && t.Result.IsOk && t.Result.Value.Count > 0)
                    Log.Message($"[RimMind] Planning: {agent.Identity.NpcId} generated {t.Result.Value.Count} schedule blocks");
            }, TaskScheduler.Current);
        }

        private void ExecuteDream(IPawnAgent agent, int pawnId)
        {
            var dreamGenerator = GetDreamGenerator();
            if (dreamGenerator?.ShouldDream(agent) != true) return;
            dreamGenerator.GenerateDreamAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Warning($"[Think] Dream generation failed for {agent.Identity.NpcId}: {t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.IsCompletedSuccessfully && t.Result.IsOk)
                {
                    GetDreamThoughtInjector()?.InjectDreamThought(pawnId, t.Result.Value);
                    _agentBus.Publish(new DreamEvent(
                        agent.Identity.NpcId, pawnId, t.Result.Value.DreamContent,
                        t.Result.Value.DreamType, t.Result.Value.MoodImpact));
                }
            }, TaskScheduler.Current);
        }

        private void ExecuteTraitEvolution(ProactiveAgentMode proactive, IPawnAgent agent, int pawnId)
        {
            var traitEngine = proactive.TraitEvolutionEngine;
            if (traitEngine?.ShouldEvolve(agent) != true) return;
            traitEngine.EvaluateEvolutionAsync(agent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Warning($"[Think] Trait evolution failed for {agent.Identity.NpcId}: {t.Exception?.InnerException?.Message}");
                    return;
                }
                if (t.IsCompletedSuccessfully && t.Result.IsOk)
                {
                    foreach (var record in t.Result.Value.Where(r => r.Confidence >= 0.7f))
                    {
                        GetTraitEvolver()?.ApplyTraitEvolution(pawnId, record);
                        _agentBus.Publish(new TraitEvolutionEvent(
                            agent.Identity.NpcId, pawnId, record.TraitDefName,
                            record.Kind, record.Reason, record.Confidence));
                    }
                }
            }, TaskScheduler.Current);
        }
    }
}
