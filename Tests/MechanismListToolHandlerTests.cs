using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Mechanisms;
using RimMind.Contracts.Result;
using RimMind.Contracts.Tools;
using RimMind.Kernel.Mechanisms;
using Xunit;

namespace RimMind.Core.Tests
{
    public class MechanismListToolHandlerTests
    {
        private class ListStubMechanism : IGameMechanism
        {
            string IExtension.Id => MechanismId;
            public string MechanismId { get; init; } = "pawn.test";
            public MechanismScope Scope { get; init; } = MechanismScope.Pawn;
            public MechanismRisk Risk { get; init; } = MechanismRisk.Safe;
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; init; }
                = new List<MechanismOperationType> { MechanismOperationType.List }.AsReadOnly();
            public MechanismDocs Docs { get; init; } = new() { Summary = "test list mechanism" };

            private readonly IReadOnlyList<MechanismEnumResult> _listResult;

            public ListStubMechanism(IReadOnlyList<MechanismEnumResult>? listResult = null)
            {
                _listResult = listResult ?? new List<MechanismEnumResult>();
            }

            public Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct)
                => Task.FromResult(Result<string, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "query")));
            public Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "set")));
            public Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "add")));
            public Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "remove")));
            public Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "toggle")));
            public Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "trigger")));
            public Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct)
                => Task.FromResult(Result<IReadOnlyList<MechanismEnumResult>, RimMindError>.Ok(_listResult));
            public Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct)
                => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "watch")));
            public IReadOnlyList<MechanismActionInfo>? GetWriteActions() => null;
            public MechanismRisk GetRiskForOperation(MechanismOperationType operation) => Risk;
        }

        [Fact]
        public void Constructor_GeneratesCorrectToolId()
        {
            var mech = new ListStubMechanism { MechanismId = "pawn.thought" };
            var handler = new MechanismListToolHandler(mech);
            Assert.Equal("pawn.thought.list", handler.Definition.Id);
        }

        [Fact]
        public void Constructor_PawnScope_SchemaHasOptionalPawnId()
        {
            var mech = new ListStubMechanism { Scope = MechanismScope.Pawn };
            var handler = new MechanismListToolHandler(mech);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["pawn_id"]);
            Assert.DoesNotContain("pawn_id", schema["required"]?.Values<string>() ?? Enumerable.Empty<string>());
        }

        [Fact]
        public void Constructor_WorldScope_SchemaHasNoPawnId()
        {
            var mech = new ListStubMechanism { MechanismId = "world.faction", Scope = MechanismScope.World };
            var handler = new MechanismListToolHandler(mech);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.Null(schema["properties"]!["pawn_id"]);
        }

        [Fact]
        public void Constructor_SchemaHasCategoryFilter()
        {
            var mech = new ListStubMechanism();
            var handler = new MechanismListToolHandler(mech);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["category"]);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsListAsJson()
        {
            var entries = new List<MechanismEnumResult>
            {
                new() { DefName = "Skill_Shooting", Label = "Shooting" },
                new() { DefName = "Skill_Melee", Label = "Melee" },
                new() { DefName = "Skill_Social", Label = "Social" },
            };
            var mech = new ListStubMechanism(entries);
            var handler = new MechanismListToolHandler(mech);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.list",
                ToolCallId = "call-list-1",
                ArgumentsJson = "{}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.False(result.Value.IsError);
            var deserialized = JsonConvert.DeserializeObject<List<MechanismEnumResult>>(result.Value.Content);
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized!.Count);
        }

        [Fact]
        public async Task ExecuteAsync_WithCategoryFilter_FiltersByDefNamePrefix()
        {
            var entries = new List<MechanismEnumResult>
            {
                new() { DefName = "Skill_Shooting", Label = "Shooting" },
                new() { DefName = "Skill_Melee", Label = "Melee" },
                new() { DefName = "Thought_Memory", Label = "Memory" },
            };
            var mech = new ListStubMechanism(entries);
            var handler = new MechanismListToolHandler(mech);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.list",
                ToolCallId = "call-list-2",
                ArgumentsJson = "{\"category\":\"Skill\"}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            var deserialized = JsonConvert.DeserializeObject<List<MechanismEnumResult>>(result.Value.Content);
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized!.Count);
            Assert.All(deserialized, item => Assert.StartsWith("Skill", item.DefName));
        }

        [Fact]
        public async Task ExecuteAsync_WithCategoryFilter_FiltersByLabelPrefix()
        {
            var entries = new List<MechanismEnumResult>
            {
                new() { DefName = "Thought_Memory", Label = "Memory" },
                new() { DefName = "Thought_Bonded", Label = "Bonded" },
                new() { DefName = "Thought_SkyHigh", Label = "Sky high" },
            };
            var mech = new ListStubMechanism(entries);
            var handler = new MechanismListToolHandler(mech);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.list",
                ToolCallId = "call-list-3",
                ArgumentsJson = "{\"category\":\"Mem\"}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            var deserialized = JsonConvert.DeserializeObject<List<MechanismEnumResult>>(result.Value.Content);
            Assert.NotNull(deserialized);
            Assert.Single(deserialized!);
            Assert.Equal("Thought_Memory", deserialized[0].DefName);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyResult_ReturnsEmptyArray()
        {
            var mech = new ListStubMechanism(new List<MechanismEnumResult>());
            var handler = new MechanismListToolHandler(mech);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.list",
                ToolCallId = "call-list-4",
                ArgumentsJson = "{}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.Equal("[]", result.Value.Content);
        }

        [Fact]
        public void Constructor_Category_IsScopeLowercased()
        {
            var mech = new ListStubMechanism { Scope = MechanismScope.World };
            var handler = new MechanismListToolHandler(mech);
            Assert.Equal("world", handler.Definition.Category);
        }
    }
}
