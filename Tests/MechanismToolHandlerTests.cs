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
    public class MechanismToolHandlerTests
    {
        private class StubMechanism : IGameMechanism
        {
            string IExtension.Id => MechanismId;
            public string MechanismId { get; }
            public MechanismScope Scope { get; }
            public MechanismRisk Risk { get; }
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
            public MechanismDocs Docs { get; }

            private readonly string _queryResult;
            private readonly bool _setResult;
            private readonly IReadOnlyList<MechanismEnumResult> _listResult;

            public StubMechanism(
                string mechanismId = "pawn.test",
                MechanismScope scope = MechanismScope.Pawn,
                MechanismRisk risk = MechanismRisk.Safe,
                IReadOnlyList<MechanismOperationType>? ops = null,
                MechanismDocs? docs = null,
                string queryResult = "query-data",
                bool setResult = true,
                IReadOnlyList<MechanismEnumResult>? listResult = null)
            {
                MechanismId = mechanismId;
                Scope = scope;
                Risk = risk;
                SupportedOperations = ops ?? new List<MechanismOperationType> { MechanismOperationType.Query, MechanismOperationType.Set, MechanismOperationType.List }.AsReadOnly();
                Docs = docs ?? new MechanismDocs { Summary = "test mechanism" };
                _queryResult = queryResult;
                _setResult = setResult;
                _listResult = listResult ?? new List<MechanismEnumResult>();
            }

            public Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct)
                => Task.FromResult(Result<string, RimMindError>.Ok(_queryResult));

            public Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct)
                => SupportedOperations.Contains(MechanismOperationType.Set)
                    ? Task.FromResult(Result<bool, RimMindError>.Ok(_setResult))
                    : Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "set")));

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
        }

        [Fact]
        public void Constructor_Query_GeneratesThreeSegmentId()
        {
            var mech = new StubMechanism("pawn.skill");
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            Assert.Equal("pawn.skill.query", handler.Definition.Id);
        }

        [Fact]
        public void Constructor_Set_GeneratesThreeSegmentId()
        {
            var mech = new StubMechanism("pawn.need");
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Set);
            Assert.Equal("pawn.need.set", handler.Definition.Id);
        }

        [Fact]
        public void Constructor_List_GeneratesThreeSegmentId()
        {
            var mech = new StubMechanism("colony.wealth", scope: MechanismScope.Colony);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.List);
            Assert.Equal("colony.wealth.list", handler.Definition.Id);
        }

        [Fact]
        public void Constructor_Category_Is_Scope_Lowercased()
        {
            var mech = new StubMechanism(scope: MechanismScope.Colony);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            Assert.Equal("colony", handler.Definition.Category);
        }

        [Fact]
        public void Constructor_Query_Schema_Has_PawnId_Required()
        {
            var mech = new StubMechanism(scope: MechanismScope.Pawn);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["pawn_id"]);
            Assert.Contains("pawn_id", schema["required"]!.Values<string>());
        }

        [Fact]
        public void Constructor_Query_Schema_HasNo_Operation_Field()
        {
            var mech = new StubMechanism();
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.Null(schema["properties"]!["operation"]);
        }

        [Fact]
        public void Constructor_DangerousRisk_Description_HasPrefix()
        {
            var mech = new StubMechanism(risk: MechanismRisk.Dangerous);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            Assert.StartsWith("[DANGEROUS]", handler.Definition.Description);
        }

        [Fact]
        public async Task ExecuteAsync_Query_ReturnsQueryResult()
        {
            var mech = new StubMechanism(queryResult: "{\"level\":10}");
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.query",
                ToolCallId = "call-1",
                ArgumentsJson = "{\"pawn_id\":5}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.Equal("{\"level\":10}", result.Value.Content);
            Assert.False(result.Value.IsError);
        }

        [Fact]
        public async Task ExecuteAsync_Set_ReturnsSetResult()
        {
            var mech = new StubMechanism();
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Set);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.set",
                ToolCallId = "call-2",
                ArgumentsJson = "{\"pawn_id\":3,\"def_name\":\"Skill_Shooting\",\"value\":\"100\"}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.Equal("True", result.Value.Content);
            Assert.False(result.Value.IsError);
        }

        [Fact]
        public async Task ExecuteAsync_List_ReturnsListResultAsJson()
        {
            var entries = new List<MechanismEnumResult>
            {
                new() { DefName = "Skill_Shooting", Label = "Shooting" },
                new() { DefName = "Skill_Melee", Label = "Melee" }
            };
            var mech = new StubMechanism(listResult: entries);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.List);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.list",
                ToolCallId = "call-3",
                ArgumentsJson = "{}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.False(result.Value.IsError);
            var deserialized = JsonConvert.DeserializeObject<List<MechanismEnumResult>>(result.Value.Content);
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized!.Count);
        }

        [Fact]
        public async Task ExecuteAsync_UnsupportedOperation_ReturnsErrorInToolResult()
        {
            var mech = new StubMechanism(ops: new List<MechanismOperationType> { MechanismOperationType.Query }.AsReadOnly());
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Set);
            var args = new ToolCallArgs
            {
                ToolId = "pawn.test.set",
                ToolCallId = "call-4",
                ArgumentsJson = "{\"pawn_id\":1}",
            };

            var result = await handler.ExecuteAsync(args, CancellationToken.None);
            Assert.True(result.IsOk);
            Assert.True(result.Value.IsError);
            Assert.Contains("not support", result.Value.Content);
        }

        [Fact]
        public void Constructor_Description_UsesDocsQueryDescription()
        {
            var docs = new MechanismDocs { Summary = "fallback", QueryDescription = "custom query desc" };
            var mech = new StubMechanism(docs: docs);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.Query);
            Assert.Equal("custom query desc", handler.Definition.Description);
        }

        [Fact]
        public void Constructor_Description_FallsBackToSummary()
        {
            var docs = new MechanismDocs { Summary = "fallback only" };
            var mech = new StubMechanism(docs: docs);
            var handler = new MechanismToolHandler(mech, MechanismOperationType.List);
            Assert.Equal("fallback only", handler.Definition.Description);
        }
    }
}
