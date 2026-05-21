using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Application.Features.Tools;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class GameMechanismRegistryTests
    {
        private class StubMechanism : IGameMechanism
        {
            string IExtension.Id => MechanismId;
            string IExtension.OwnerModId => "Test";
            public string MechanismId { get; }
            public MechanismScope Scope { get; }
            public MechanismRisk Risk { get; }
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
            public MechanismDocs Docs { get; }

            public StubMechanism(
                string mechanismId,
                MechanismScope scope = MechanismScope.Pawn,
                MechanismRisk risk = MechanismRisk.Safe,
                IReadOnlyList<MechanismOperationType>? ops = null,
                MechanismDocs? docs = null)
            {
                MechanismId = mechanismId;
                Scope = scope;
                Risk = risk;
                SupportedOperations = ops ?? new List<MechanismOperationType> { MechanismOperationType.Query }.AsReadOnly();
                Docs = docs ?? new MechanismDocs { Summary = "test" };
            }

            public Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct)
                => Task.FromResult(Result<string, RimMindError>.Ok("stub"));

            public Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Ok(true));

            public Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "add")));

            public Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "remove")));

            public Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "toggle")));

            public Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "trigger")));

            public Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct)
                => Task.FromResult(Result<IReadOnlyList<MechanismEnumResult>, RimMindError>.Ok(new List<MechanismEnumResult>().AsReadOnly()));

            public Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "watch")));

            public IReadOnlyList<MechanismActionInfo>? GetWriteActions() => null;

            public MechanismRisk GetRiskForOperation(MechanismOperationType operation) => Risk;
        }

        [Fact]
        public void Register_AddsMechanism_And_FindById_ReturnsIt()
        {
            var registry = new GameMechanismRegistry();
            var mechanism = new StubMechanism("pawn.skill");
            registry.Register(mechanism);
            Assert.Same(mechanism, registry.FindById("pawn.skill"));
        }

        [Fact]
        public void Register_NullMechanism_DoesNotThrow()
        {
            var registry = new GameMechanismRegistry();
            var ex = Record.Exception(() => registry.Register(null!));
            Assert.Null(ex);
        }

        [Fact]
        public void FindById_ReturnsNull_ForUnknownMechanism()
        {
            var registry = new GameMechanismRegistry();
            Assert.Null(registry.FindById("nonexistent"));
        }

        [Fact]
        public void Unregister_RemovesMechanism_And_ReturnsTrue()
        {
            var registry = new GameMechanismRegistry();
            var mechanism = new StubMechanism("pawn.skill");
            registry.Register(mechanism);
            var result = registry.Unregister("pawn.skill");
            Assert.True(result);
            Assert.Null(registry.FindById("pawn.skill"));
        }

        [Fact]
        public void Unregister_ReturnsFalse_ForUnknownMechanism()
        {
            var registry = new GameMechanismRegistry();
            Assert.False(registry.Unregister("nonexistent"));
        }

        [Fact]
        public void All_ReturnsAllRegisteredMechanisms()
        {
            var registry = new GameMechanismRegistry();
            var m1 = new StubMechanism("pawn.skill");
            var m2 = new StubMechanism("pawn.need");
            var m3 = new StubMechanism("colony.wealth", scope: MechanismScope.Colony);
            registry.Register(m1);
            registry.Register(m2);
            registry.Register(m3);
            var all = registry.All;
            Assert.Equal(3, all.Count);
        }

        [Fact]
        public void All_ReturnsEmptyList_WhenNoMechanisms()
        {
            var registry = new GameMechanismRegistry();
            Assert.Empty(registry.All);
        }

        [Fact]
        public void Register_WithSameId_ReplacesExistingMechanism()
        {
            var registry = new GameMechanismRegistry();
            var original = new StubMechanism("pawn.skill");
            var replacement = new StubMechanism("pawn.skill");
            registry.Register(original);
            registry.Register(replacement);
            var result = registry.FindById("pawn.skill");
            Assert.Same(replacement, result);
            Assert.Single(registry.All);
        }

        [Fact]
        public void Register_WithToolRegistry_AutoExpandsToToolHandlers()
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            var mechanism = new StubMechanism("pawn.skill", ops:
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly());

            mechanismRegistry.Register(mechanism);

            Assert.NotNull(toolRegistry.FindById("pawn.skill.query"));
            Assert.NotNull(toolRegistry.FindById("pawn.skill.set"));
            Assert.NotNull(toolRegistry.FindById("pawn.skill.list"));
            Assert.Null(toolRegistry.FindById("pawn.skill.add"));
        }

        [Fact]
        public void Unregister_WithToolRegistry_RemovesExpandedToolHandlers()
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            var mechanism = new StubMechanism("pawn.need", ops:
                new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set }.AsReadOnly());

            mechanismRegistry.Register(mechanism);
            Assert.NotNull(toolRegistry.FindById("pawn.need.query"));
            Assert.NotNull(toolRegistry.FindById("pawn.need.set"));

            mechanismRegistry.Unregister("pawn.need");
            Assert.Null(toolRegistry.FindById("pawn.need.query"));
            Assert.Null(toolRegistry.FindById("pawn.need.set"));
        }
    }
}
