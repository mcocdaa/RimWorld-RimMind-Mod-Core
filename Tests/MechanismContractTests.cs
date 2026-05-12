using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Result;
using RimMind.Contracts.Tools;
using RimMind.Kernel.Mechanisms;
using RimMind.Kernel.Tools;
using Xunit;

namespace RimMind.Core.Tests
{
    public class MechanismContractTests
    {
        private static readonly List<MechanismContract> _h2Mechanisms = new()
        {
            new("pawn.job", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set }.AsReadOnly()),
            new("pawn.draft", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Toggle }.AsReadOnly()),
            new("pawn.work", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.equipment", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set }.AsReadOnly()),
            new("pawn.interaction", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Trigger }.AsReadOnly()),
            new("pawn.recruit", MechanismScope.Pawn, MechanismRisk.Dangerous,
                new List<MechanismOperationType> { MechanismOperationType.Trigger }.AsReadOnly()),
            new("pawn.thought", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Add, MechanismOperationType.Remove, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.inspiration", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Trigger, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.mental_state", MechanismScope.Pawn, MechanismRisk.Dangerous,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Trigger, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.health", MechanismScope.Pawn, MechanismRisk.Safe,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.relations", MechanismScope.Pawn, MechanismRisk.Safe,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.skill", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly()),
            new("pawn.need", MechanismScope.Pawn, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly()),
            new("map.wealth", MechanismScope.Map, MechanismRisk.Safe,
                new List<MechanismOperationType> { MechanismOperationType.Query }.AsReadOnly()),
            new("world.faction", MechanismScope.World, MechanismRisk.Dangerous,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly()),
            new("world.storyteller", MechanismScope.World, MechanismRisk.Dangerous,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Trigger, MechanismOperationType.List }.AsReadOnly()),
            new("world.choice_letter", MechanismScope.World, MechanismRisk.Moderate,
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Trigger }.AsReadOnly()),
        };

        public static IEnumerable<object[]> AllMechanismContracts =>
            _h2Mechanisms.Select(m => new object[] { m });

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void MechanismId_FollowsTwoSegmentPattern(MechanismContract contract)
        {
            var segments = contract.MechanismId.Split('.');
            Assert.True(segments.Length == 2,
                $"MechanismId '{contract.MechanismId}' should be two segments (scope.mechanism), got {segments.Length}");
        }

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void MechanismId_ScopePrefix_MatchesDeclaredScope(MechanismContract contract)
        {
            var scopePrefix = contract.MechanismId.Split('.')[0];
            var expectedPrefix = contract.Scope.ToString().ToLowerInvariant();
            if (contract.Scope == MechanismScope.Pawn) expectedPrefix = "pawn";
            if (contract.Scope == MechanismScope.Map) expectedPrefix = "map";
            if (contract.Scope == MechanismScope.World) expectedPrefix = "world";
            Assert.Equal(expectedPrefix, scopePrefix);
        }

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void SupportedOperations_NotEmpty(MechanismContract contract)
        {
            Assert.NotEmpty(contract.SupportedOperations);
        }

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void Risk_IsValidEnumValue(MechanismContract contract)
        {
            Assert.True(contract.Risk == MechanismRisk.Safe ||
                        contract.Risk == MechanismRisk.Moderate ||
                        contract.Risk == MechanismRisk.Dangerous,
                $"Risk '{contract.Risk}' is not a valid MechanismRisk value");
        }

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void EachOperation_GeneratesCorrectToolId(MechanismContract contract)
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            var stub = new ContractStubMechanism(contract);

            mechanismRegistry.Register(stub);

            foreach (var op in contract.SupportedOperations)
            {
                var expectedToolId = $"{contract.MechanismId}.{op.ToString().ToLowerInvariant()}";
                var handler = toolRegistry.FindById(expectedToolId);
                Assert.NotNull(handler);
            }
        }

        [Theory]
        [MemberData(nameof(AllMechanismContracts))]
        public void UnregisteredOperation_NoToolHandler(MechanismContract contract)
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            var stub = new ContractStubMechanism(contract);

            mechanismRegistry.Register(stub);

            var allOps = new[] {
                MechanismOperationType.Query, MechanismOperationType.Set,
                MechanismOperationType.Add, MechanismOperationType.Remove,
                MechanismOperationType.Toggle, MechanismOperationType.Trigger,
                MechanismOperationType.List, MechanismOperationType.Watch
            };

            foreach (var op in allOps)
            {
                var toolId = $"{contract.MechanismId}.{op.ToString().ToLowerInvariant()}";
                var handler = toolRegistry.FindById(toolId);
                if (contract.SupportedOperations.Contains(op))
                    Assert.NotNull(handler);
                else
                    Assert.Null(handler);
            }
        }

        [Fact]
        public void H2_ShouldHave17Mechanisms()
        {
            Assert.Equal(17, _h2Mechanisms.Count);
        }

        [Fact]
        public void PawnScope_ShouldHave13Mechanisms()
        {
            var pawnMechanisms = _h2Mechanisms.Where(m => m.Scope == MechanismScope.Pawn).ToList();
            Assert.Equal(13, pawnMechanisms.Count);
        }

        [Fact]
        public void MapScope_ShouldHave1Mechanism()
        {
            var mapMechanisms = _h2Mechanisms.Where(m => m.Scope == MechanismScope.Map).ToList();
            Assert.Single(mapMechanisms);
        }

        [Fact]
        public void WorldScope_ShouldHave3Mechanisms()
        {
            var worldMechanisms = _h2Mechanisms.Where(m => m.Scope == MechanismScope.World).ToList();
            Assert.Equal(3, worldMechanisms.Count);
        }

        [Fact]
        public void AllMechanismIds_AreUnique()
        {
            var ids = _h2Mechanisms.Select(m => m.MechanismId).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void DangerousMechanisms_ShouldHaveAtLeastOneWriteOperation()
        {
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };

            foreach (var m in _h2Mechanisms.Where(m => m.Risk == MechanismRisk.Dangerous))
            {
                Assert.True(m.SupportedOperations.Any(op => writeOps.Contains(op)),
                    $"Dangerous mechanism '{m.MechanismId}' should have at least one write operation");
            }
        }

        [Fact]
        public void SafeMechanisms_ShouldOnlyHaveReadOperations()
        {
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };

            foreach (var m in _h2Mechanisms.Where(m => m.Risk == MechanismRisk.Safe))
            {
                Assert.True(m.SupportedOperations.All(op => !writeOps.Contains(op)),
                    $"Safe mechanism '{m.MechanismId}' should only have read operations, but has: {string.Join(", ", m.SupportedOperations)}");
            }
        }

        [Fact]
        public void RegisterAll17Mechanisms_ToolCountShouldBeAtLeast25()
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);

            foreach (var contract in _h2Mechanisms)
            {
                mechanismRegistry.Register(new ContractStubMechanism(contract));
            }

            var toolCount = toolRegistry.All.Count;
            Assert.True(toolCount >= 25,
                $"Expected at least 25 tools from 17 mechanisms, got {toolCount}");
        }

        [Fact]
        public void QueryOperation_PawnScope_HasPawnIdInSchema()
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);

            var pawnQueryMechanisms = _h2Mechanisms
                .Where(m => m.Scope == MechanismScope.Pawn && m.SupportedOperations.Contains(MechanismOperationType.Query));

            foreach (var contract in pawnQueryMechanisms)
            {
                mechanismRegistry.Register(new ContractStubMechanism(contract));
                var toolId = $"{contract.MechanismId}.query";
                var handler = toolRegistry.FindById(toolId);
                Assert.NotNull(handler);
                Assert.Contains("pawn_id", handler.Definition.ParametersSchema);
            }
        }

        private class ContractStubMechanism : IGameMechanism
        {
            string IExtension.Id => MechanismId;
            public string MechanismId { get; }
            public MechanismScope Scope { get; }
            public MechanismRisk Risk { get; }
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
            public MechanismDocs Docs { get; }

            public ContractStubMechanism(MechanismContract contract)
            {
                MechanismId = contract.MechanismId;
                Scope = contract.Scope;
                Risk = contract.Risk;
                SupportedOperations = contract.SupportedOperations;
                Docs = new MechanismDocs { Summary = $"{contract.MechanismId} test stub" };
            }

            public Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct)
                => Task.FromResult(Result<string, RimMindError>.Ok("stub"));
            public Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct)
                => Task.FromResult(Result<IReadOnlyList<MechanismEnumResult>, RimMindError>.Ok(new List<MechanismEnumResult>().AsReadOnly()));
            public Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));
            public IReadOnlyList<MechanismActionInfo>? GetWriteActions() => null;
            public MechanismRisk GetRiskForOperation(MechanismOperationType operation) => Risk;
        }
    }

    public record MechanismContract(
        string MechanismId,
        MechanismScope Scope,
        MechanismRisk Risk,
        IReadOnlyList<MechanismOperationType> SupportedOperations);
}
