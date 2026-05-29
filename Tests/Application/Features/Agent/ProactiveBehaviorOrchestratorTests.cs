using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Planning;
using RimMind.Application.Common.Interfaces.Agent.Reflection;
using RimMind.Application.Common.Interfaces.Agent.Social;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Planning;
using RimMind.Domain.Agent.Reflection;
using RimMind.Domain.Agent.Social;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent
{
    public class ProactiveBehaviorOrchestratorTests
    {
        private readonly StubAgentBus _bus = new();
        private readonly StubAgentInfo _agent = new() { NpcId = "npc-1" };
        private const int PawnId = 42;

        [Fact]
        public void ExecuteReflection_WhenShouldReflectFalse_DoesNotCallReflectAsync()
        {
            var strategy = new StubReflectionStrategy { ShouldReflectResult = false };
            var sut = MakeSut(reflectionStrategy: strategy);
            sut.ExecuteReflection(_agent);
            Assert.False(strategy.ReflectAsyncCalled);
        }

        [Fact]
        public void ExecuteReflection_WhenShouldReflectTrue_CallsReflectAsync()
        {
            var strategy = new StubReflectionStrategy { ShouldReflectResult = true };
            var sut = MakeSut(reflectionStrategy: strategy);
            sut.ExecuteReflection(_agent);
            Assert.True(strategy.ReflectAsyncCalled);
        }

        [Fact]
        public void ExecutePlanning_WhenShouldPlanFalse_DoesNotCallPlanAsync()
        {
            var planner = new StubDailyPlanner { ShouldPlanResult = false };
            var sut = MakeSut(dailyPlanner: planner);
            sut.ExecutePlanning(_agent);
            Assert.False(planner.PlanAsyncCalled);
        }

        [Fact]
        public void ExecutePlanning_WhenShouldPlanTrue_CallsPlanAsync()
        {
            var planner = new StubDailyPlanner { ShouldPlanResult = true };
            var sut = MakeSut(dailyPlanner: planner);
            sut.ExecutePlanning(_agent);
            Assert.True(planner.PlanAsyncCalled);
        }

        [Fact]
        public void ExecuteDream_WhenShouldDreamFalse_DoesNotCallGenerateDreamAsync()
        {
            var generator = new StubDreamGenerator { ShouldDreamResult = false };
            var sut = MakeSut(dreamGenerator: generator);
            sut.ExecuteDream(_agent);
            Assert.False(generator.GenerateDreamAsyncCalled);
        }

        [Fact]
        public void ExecuteDream_WhenShouldDreamTrue_CallsGenerateDreamAsync()
        {
            var generator = new StubDreamGenerator { ShouldDreamResult = true };
            var sut = MakeSut(dreamGenerator: generator);
            sut.ExecuteDream(_agent);
            Assert.True(generator.GenerateDreamAsyncCalled);
        }

        [Fact]
        public void ExecuteTraitEvolution_WhenShouldEvolveFalse_DoesNotCallEvaluateEvolutionAsync()
        {
            var engine = new StubTraitEvolutionEngine { ShouldEvolveResult = false };
            var sut = MakeSut(traitEvolutionEngine: engine);
            sut.ExecuteTraitEvolution(_agent);
            Assert.False(engine.EvaluateEvolutionAsyncCalled);
        }

        [Fact]
        public void ExecuteTraitEvolution_WhenShouldEvolveTrue_CallsEvaluateEvolutionAsync()
        {
            var engine = new StubTraitEvolutionEngine { ShouldEvolveResult = true };
            var sut = MakeSut(traitEvolutionEngine: engine);
            sut.ExecuteTraitEvolution(_agent);
            Assert.True(engine.EvaluateEvolutionAsyncCalled);
        }

        [Fact]
        public void Constructor_NullAgentBus_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProactiveBehaviorOrchestrator(
                null, null, null, null, null, null, null!, PawnId));
        }

        [Fact]
        public void ExecuteReflection_NullStrategy_DoesNothing()
        {
            var sut = MakeSut(reflectionStrategy: null);
            sut.ExecuteReflection(_agent);
        }

        [Fact]
        public void ExecutePlanning_NullPlanner_DoesNothing()
        {
            var sut = MakeSut(dailyPlanner: null);
            sut.ExecutePlanning(_agent);
        }

        [Fact]
        public void ExecuteDream_NullGenerator_DoesNothing()
        {
            var sut = MakeSut(dreamGenerator: null);
            sut.ExecuteDream(_agent);
        }

        [Fact]
        public void ExecuteTraitEvolution_NullEngine_DoesNothing()
        {
            var sut = MakeSut(traitEvolutionEngine: null);
            sut.ExecuteTraitEvolution(_agent);
        }

        [Fact]
        public void TraitConfidenceThreshold_Is_0_7()
        {
            var record = new TraitEvolutionRecord { Confidence = 0.69f, TraitDefName = "T", Kind = TraitEvolutionKind.Gained, Reason = "r" };
            var engine = new StubTraitEvolutionEngine
            {
                ShouldEvolveResult = true,
                Records = new List<TraitEvolutionRecord> { record }
            };
            var evolver = new StubTraitEvolver();
            var sut = MakeSut(traitEvolutionEngine: engine, traitEvolver: evolver);
            sut.ExecuteTraitEvolution(_agent);
            Assert.Empty(evolver.AppliedRecords);
        }

        [Fact]
        public void TraitConfidence_AboveThreshold_AppliesEvolution()
        {
            var record = new TraitEvolutionRecord { Confidence = 0.7f, TraitDefName = "T", Kind = TraitEvolutionKind.Gained, Reason = "r" };
            var engine = new StubTraitEvolutionEngine
            {
                ShouldEvolveResult = true,
                Records = new List<TraitEvolutionRecord> { record }
            };
            var evolver = new StubTraitEvolver();
            var sut = MakeSut(traitEvolutionEngine: engine, traitEvolver: evolver);
            sut.ExecuteTraitEvolution(_agent);
            Assert.Single(evolver.AppliedRecords);
        }

        private ProactiveBehaviorOrchestrator MakeSut(
            IReflectionStrategy? reflectionStrategy = null,
            IDailyPlanner? dailyPlanner = null,
            IDreamGenerator? dreamGenerator = null,
            IDreamThoughtInjector? dreamThoughtInjector = null,
            ITraitEvolutionEngine? traitEvolutionEngine = null,
            ITraitEvolver? traitEvolver = null)
        {
            return new ProactiveBehaviorOrchestrator(
                reflectionStrategy, dailyPlanner, dreamGenerator, dreamThoughtInjector,
                traitEvolutionEngine, traitEvolver, _bus, PawnId);
        }

        private class StubReflectionStrategy : IReflectionStrategy
        {
            public bool ShouldReflectResult { get; set; }
            public bool ReflectAsyncCalled { get; private set; }
            public bool ShouldReflect(IAgentInfo agent) => ShouldReflectResult;
            public Task<Result<IReadOnlyList<ReflectionEntry>, RimMindError>> ReflectAsync(
                IAgentInfo agent, CancellationToken ct = default)
            {
                ReflectAsyncCalled = true;
                return Task.FromResult(Result<IReadOnlyList<ReflectionEntry>, RimMindError>.Ok(Array.Empty<ReflectionEntry>()));
            }
        }

        private class StubDailyPlanner : IDailyPlanner
        {
            public bool ShouldPlanResult { get; set; }
            public bool PlanAsyncCalled { get; private set; }
            public bool ShouldPlan(IAgentInfo agent) => ShouldPlanResult;
            public Task<Result<IReadOnlyList<ScheduleBlock>, RimMindError>> PlanAsync(
                IAgentInfo agent, CancellationToken ct = default)
            {
                PlanAsyncCalled = true;
                return Task.FromResult(Result<IReadOnlyList<ScheduleBlock>, RimMindError>.Ok(Array.Empty<ScheduleBlock>()));
            }
        }

        private class StubDreamGenerator : IDreamGenerator
        {
            public bool ShouldDreamResult { get; set; }
            public bool GenerateDreamAsyncCalled { get; private set; }
            public bool ShouldDream(IAgentInfo agent) => ShouldDreamResult;
            public Task<Result<DreamEntry, RimMindError>> GenerateDreamAsync(
                IAgentInfo agent, CancellationToken ct = default)
            {
                GenerateDreamAsyncCalled = true;
                var entry = new DreamEntry { DreamContent = "d", DreamType = DreamType.Recollection, MoodImpact = 0.1f };
                return Task.FromResult(Result<DreamEntry, RimMindError>.Ok(entry));
            }
        }

        private class StubTraitEvolutionEngine : ITraitEvolutionEngine
        {
            public bool ShouldEvolveResult { get; set; }
            public bool EvaluateEvolutionAsyncCalled { get; private set; }
            public IReadOnlyList<TraitEvolutionRecord> Records { get; set; } = Array.Empty<TraitEvolutionRecord>();
            public bool ShouldEvolve(IAgentInfo agent) => ShouldEvolveResult;
            public Task<Result<IReadOnlyList<TraitEvolutionRecord>, RimMindError>> EvaluateEvolutionAsync(
                IAgentInfo agent, CancellationToken ct = default)
            {
                EvaluateEvolutionAsyncCalled = true;
                return Task.FromResult(Result<IReadOnlyList<TraitEvolutionRecord>, RimMindError>.Ok(Records));
            }
        }

        private class StubTraitEvolver : ITraitEvolver
        {
            public List<TraitEvolutionRecord> AppliedRecords { get; } = new();
            public Result<TraitEvolutionRecord, RimMindError> ApplyTraitEvolution(int pawnId, TraitEvolutionRecord record)
            {
                AppliedRecords.Add(record);
                return Result<TraitEvolutionRecord, RimMindError>.Ok(record);
            }
        }
    }
}
