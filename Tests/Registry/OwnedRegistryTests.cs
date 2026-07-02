using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Registry;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Context;
using RimMind.Application.Features.Tools;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Mechanisms;
using Xunit;

namespace RimMind.Core.Tests.Registry
{
    /// <summary>
    /// IOwnedRegistry.UnregisterByOwner 契约测试，覆盖 3 个实现注册表：
    /// ToolRegistry、GameMechanismRegistry、ContextKeyRegistryImpl。
    /// 其他 3 个注册表（SchemaRegistry/ProviderRegistry/DebugCenterPageRegistry）
    /// 项类型无 OwnerModId 概念，不实现 IOwnedRegistry，不在此测试范围。
    /// </summary>
    public class OwnedRegistryTests
    {
        // ========== ToolRegistry ==========

        private class StubToolHandler : IToolHandler
        {
            private readonly string _ownerModId;
            string IExtension.Id => Definition.Id;
            string IExtension.OwnerModId => _ownerModId;
            public ToolDefinition Definition { get; }

            public StubToolHandler(string id, string ownerModId)
            {
                _ownerModId = ownerModId;
                Definition = new ToolDefinition { Id = id };
            }

            public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
                => Task.FromResult(Result<ToolResult, RimMindError>.Ok(new ToolResult { ToolCallId = args.ToolCallId, Content = "stub" }));
        }

        public class ToolRegistryUnregisterByOwnerTests
        {
            [Fact]
            public void Null_Owner_Throws()
            {
                var registry = new ToolRegistry();
                Assert.Throws<ArgumentNullException>(() => registry.UnregisterByOwner(null!));
            }

            [Fact]
            public void Empty_Registry_Returns_Zero()
            {
                var registry = new ToolRegistry();
                Assert.Equal(0, registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId));
            }

            [Fact]
            public void Removes_All_Items_Of_Owner_Keeps_Others()
            {
                var registry = new ToolRegistry();
                var core1 = new StubToolHandler("core.1", RimMindOwnerConsts.CoreModId);
                var core2 = new StubToolHandler("core.2", RimMindOwnerConsts.CoreModId);
                var other = new StubToolHandler("other.1", "OtherMod");
                registry.Register(core1);
                registry.Register(core2);
                registry.Register(other);

                var removed = registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId);

                Assert.Equal(2, removed);
                Assert.Null(registry.FindById("core.1"));
                Assert.Null(registry.FindById("core.2"));
                Assert.NotNull(registry.FindById("other.1"));
            }

            [Fact]
            public void NonExistent_Owner_Returns_Zero()
            {
                var registry = new ToolRegistry();
                registry.Register(new StubToolHandler("a", RimMindOwnerConsts.CoreModId));
                Assert.Equal(0, registry.UnregisterByOwner("NonExistent"));
            }
        }

        // ========== GameMechanismRegistry ==========

        private class StubMechanism : IGameMechanism
        {
            private readonly string _ownerModId;
            string IExtension.Id => MechanismId;
            string IExtension.OwnerModId => _ownerModId;
            public string MechanismId { get; }
            public MechanismScope Scope => MechanismScope.Pawn;
            public MechanismRisk Risk => MechanismRisk.Safe;
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
            public MechanismDocs Docs { get; } = new MechanismDocs { Summary = "test" };

            public StubMechanism(string mechanismId, string ownerModId)
            {
                MechanismId = mechanismId;
                _ownerModId = ownerModId;
                SupportedOperations = new List<MechanismOperationType> { MechanismOperationType.Query }.AsReadOnly();
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

        public class GameMechanismRegistryUnregisterByOwnerTests
        {
            [Fact]
            public void Null_Owner_Throws()
            {
                var registry = new GameMechanismRegistry();
                Assert.Throws<ArgumentNullException>(() => registry.UnregisterByOwner(null!));
            }

            [Fact]
            public void Empty_Registry_Returns_Zero()
            {
                var registry = new GameMechanismRegistry();
                Assert.Equal(0, registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId));
            }

            [Fact]
            public void Removes_All_Items_Of_Owner_Keeps_Others()
            {
                var registry = new GameMechanismRegistry();
                var core1 = new StubMechanism("core.skill", RimMindOwnerConsts.CoreModId);
                var core2 = new StubMechanism("core.job", RimMindOwnerConsts.CoreModId);
                var other = new StubMechanism("other.foo", "OtherMod");
                registry.Register(core1);
                registry.Register(core2);
                registry.Register(other);

                var removed = registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId);

                Assert.Equal(2, removed);
                Assert.Null(registry.FindById("core.skill"));
                Assert.Null(registry.FindById("core.job"));
                Assert.NotNull(registry.FindById("other.foo"));
            }

            [Fact]
            public void NonExistent_Owner_Returns_Zero()
            {
                var registry = new GameMechanismRegistry();
                registry.Register(new StubMechanism("a", RimMindOwnerConsts.CoreModId));
                Assert.Equal(0, registry.UnregisterByOwner("NonExistent"));
            }
        }

        // ========== ContextKeyRegistryImpl ==========

        public class ContextKeyRegistryImplUnregisterByOwnerTests
        {
            private static KeyMeta MakeKey(string key, string ownerMod)
            {
                return new KeyMeta(key, ContextLayer.L1_Baseline, 0, _ => new List<ContextEntry>(), ownerMod);
            }

            [Fact]
            public void Null_Owner_Throws()
            {
                var registry = new ContextKeyRegistryImpl();
                Assert.Throws<ArgumentNullException>(() => registry.UnregisterByOwner(null!));
            }

            [Fact]
            public void Empty_Registry_Returns_Zero()
            {
                var registry = new ContextKeyRegistryImpl();
                Assert.Equal(0, registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId));
            }

            [Fact]
            public void Removes_All_Items_Of_Owner_Keeps_Others()
            {
                var registry = new ContextKeyRegistryImpl();
                registry.Register(MakeKey("core.1", RimMindOwnerConsts.CoreModId));
                registry.Register(MakeKey("core.2", RimMindOwnerConsts.CoreModId));
                registry.Register(MakeKey("other.1", "OtherMod"));

                var removed = registry.UnregisterByOwner(RimMindOwnerConsts.CoreModId);

                Assert.Equal(2, removed);
                Assert.Null(registry.Get("core.1"));
                Assert.Null(registry.Get("core.2"));
                Assert.NotNull(registry.Get("other.1"));
            }

            [Fact]
            public void NonExistent_Owner_Returns_Zero()
            {
                var registry = new ContextKeyRegistryImpl();
                registry.Register(MakeKey("a", RimMindOwnerConsts.CoreModId));
                Assert.Equal(0, registry.UnregisterByOwner("NonExistent"));
            }
        }
    }
}
