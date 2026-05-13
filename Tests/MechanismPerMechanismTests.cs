using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Domain.Events.Extension;
using RimMind.Domain.Events.Mechanisms;
using RimMind.Domain.Events.Result;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Application.Features.Tools;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class MechanismPerMechanismTests
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

        private (ToolRegistry toolRegistry, GameMechanismRegistry mechRegistry) CreateRegistries()
        {
            var toolRegistry = new ToolRegistry();
            var mechanismRegistry = new GameMechanismRegistry(toolRegistry);
            return (toolRegistry, mechanismRegistry);
        }

        private void RegisterAll(GameMechanismRegistry registry)
        {
            foreach (var contract in _h2Mechanisms)
                registry.Register(new PerMechanismStub(contract));
        }

        // ─── pawn.job ───────────────────────────────────────────
        [Fact]
        public void PawnJob_RegistersQueryAndSetTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[0]));
            Assert.NotNull(tools.FindById("pawn.job.query"));
            Assert.NotNull(tools.FindById("pawn.job.set"));
        }

        [Fact]
        public void PawnJob_SetTool_HasActionFieldInSchema()
        {
            var (tools, mechs) = CreateRegistries();
            var stub = new PerMechanismStub(_h2Mechanisms[0], writeActions: new List<MechanismActionInfo>
            {
                new() { Action = "assign_work", Description = "assign work to pawn" },
                new() { Action = "move_to", Description = "move pawn to location" },
            });
            mechs.Register(stub);
            var handler = tools.FindById("pawn.job.set");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["action"]);
        }

        // ─── pawn.draft ─────────────────────────────────────────
        [Fact]
        public void PawnDraft_RegistersQueryAndToggleTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[1]));
            Assert.NotNull(tools.FindById("pawn.draft.query"));
            Assert.NotNull(tools.FindById("pawn.draft.toggle"));
        }

        [Fact]
        public void PawnDraft_ToggleTool_HasPawnIdRequired()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[1]));
            var handler = tools.FindById("pawn.draft.toggle");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.Contains("pawn_id", schema["required"]!.Values<string>());
        }

        // ─── pawn.work ──────────────────────────────────────────
        [Fact]
        public void PawnWork_RegistersQuerySetListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[2]));
            Assert.NotNull(tools.FindById("pawn.work.query"));
            Assert.NotNull(tools.FindById("pawn.work.set"));
            Assert.NotNull(tools.FindById("pawn.work.list"));
        }

        [Fact]
        public void PawnWork_ListTool_HasCategoryFilter()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[2]));
            var listHandler = tools.FindById("pawn.work.list");
            Assert.NotNull(listHandler);
            var schema = JObject.Parse(listHandler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["category"]);
        }

        // ─── pawn.equipment ─────────────────────────────────────
        [Fact]
        public void PawnEquipment_RegistersQueryAndSetTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[3]));
            Assert.NotNull(tools.FindById("pawn.equipment.query"));
            Assert.NotNull(tools.FindById("pawn.equipment.set"));
        }

        [Fact]
        public void PawnEquipment_SetTool_HasActionField()
        {
            var (tools, mechs) = CreateRegistries();
            var stub = new PerMechanismStub(_h2Mechanisms[3], writeActions: new List<MechanismActionInfo>
            {
                new() { Action = "drop_weapon", Description = "drop current weapon" },
            });
            mechs.Register(stub);
            var handler = tools.FindById("pawn.equipment.set");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["action"]);
        }

        // ─── pawn.interaction ────────────────────────────────────
        [Fact]
        public void PawnInteraction_RegistersQueryAndTriggerTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[4]));
            Assert.NotNull(tools.FindById("pawn.interaction.query"));
            Assert.NotNull(tools.FindById("pawn.interaction.trigger"));
        }

        [Fact]
        public void PawnInteraction_TriggerTool_HasActionField()
        {
            var (tools, mechs) = CreateRegistries();
            var stub = new PerMechanismStub(_h2Mechanisms[4], writeActions: new List<MechanismActionInfo>
            {
                new() { Action = "social_relax", Description = "initiate social relaxation" },
                new() { Action = "romance_attempt", Description = "attempt romance" },
            });
            mechs.Register(stub);
            var handler = tools.FindById("pawn.interaction.trigger");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.NotNull(schema["properties"]!["action"]);
        }

        // ─── pawn.recruit ───────────────────────────────────────
        [Fact]
        public void PawnRecruit_RegistersOnlyTriggerTool()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[5]));
            Assert.NotNull(tools.FindById("pawn.recruit.trigger"));
            Assert.Null(tools.FindById("pawn.recruit.query"));
        }

        [Fact]
        public void PawnRecruit_IsDangerousRisk()
        {
            Assert.Equal(MechanismRisk.Dangerous, _h2Mechanisms[5].Risk);
        }

        // ─── pawn.thought ───────────────────────────────────────
        [Fact]
        public void PawnThought_RegistersQueryAddRemoveListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[6]));
            Assert.NotNull(tools.FindById("pawn.thought.query"));
            Assert.NotNull(tools.FindById("pawn.thought.add"));
            Assert.NotNull(tools.FindById("pawn.thought.list"));
        }

        [Fact]
        public void PawnThought_ListTool_ExistsForLargeEnum()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[6]));
            Assert.NotNull(tools.FindById("pawn.thought.list"));
        }

        // ─── pawn.inspiration ───────────────────────────────────
        [Fact]
        public void PawnInspiration_RegistersQueryTriggerListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[7]));
            Assert.NotNull(tools.FindById("pawn.inspiration.query"));
            Assert.NotNull(tools.FindById("pawn.inspiration.trigger"));
            Assert.NotNull(tools.FindById("pawn.inspiration.list"));
        }

        [Fact]
        public void PawnInspiration_IsModerateRisk()
        {
            Assert.Equal(MechanismRisk.Moderate, _h2Mechanisms[7].Risk);
        }

        // ─── pawn.mental_state ──────────────────────────────────
        [Fact]
        public void PawnMentalState_RegistersQueryTriggerListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[8]));
            Assert.NotNull(tools.FindById("pawn.mental_state.query"));
            Assert.NotNull(tools.FindById("pawn.mental_state.trigger"));
            Assert.NotNull(tools.FindById("pawn.mental_state.list"));
        }

        [Fact]
        public void PawnMentalState_IsDangerousRisk()
        {
            Assert.Equal(MechanismRisk.Dangerous, _h2Mechanisms[8].Risk);
        }

        // ─── pawn.health ────────────────────────────────────────
        [Fact]
        public void PawnHealth_RegistersQueryAndListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[9]));
            Assert.NotNull(tools.FindById("pawn.health.query"));
            Assert.NotNull(tools.FindById("pawn.health.list"));
        }

        [Fact]
        public void PawnHealth_IsSafeRisk_ReadOnlyOperations()
        {
            Assert.Equal(MechanismRisk.Safe, _h2Mechanisms[9].Risk);
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };
            Assert.All(_h2Mechanisms[9].SupportedOperations, op => Assert.DoesNotContain(op, writeOps));
        }

        // ─── pawn.relations ─────────────────────────────────────
        [Fact]
        public void PawnRelations_RegistersOnlyQueryTool()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[10]));
            Assert.NotNull(tools.FindById("pawn.relations.query"));
            Assert.Null(tools.FindById("pawn.relations.set"));
        }

        [Fact]
        public void PawnRelations_IsSafeRisk_ReadOnlyOperations()
        {
            Assert.Equal(MechanismRisk.Safe, _h2Mechanisms[10].Risk);
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };
            Assert.All(_h2Mechanisms[10].SupportedOperations, op => Assert.DoesNotContain(op, writeOps));
        }

        // ─── pawn.skill ─────────────────────────────────────────
        [Fact]
        public void PawnSkill_RegistersQuerySetListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[11]));
            Assert.NotNull(tools.FindById("pawn.skill.query"));
            Assert.NotNull(tools.FindById("pawn.skill.set"));
            Assert.NotNull(tools.FindById("pawn.skill.list"));
        }

        [Fact]
        public void PawnSkill_SetTool_HasPawnIdRequired()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[11]));
            var handler = tools.FindById("pawn.skill.set");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.Contains("pawn_id", schema["required"]!.Values<string>());
        }

        // ─── pawn.need ──────────────────────────────────────────
        [Fact]
        public void PawnNeed_RegistersQuerySetListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[12]));
            Assert.NotNull(tools.FindById("pawn.need.query"));
            Assert.NotNull(tools.FindById("pawn.need.set"));
            Assert.NotNull(tools.FindById("pawn.need.list"));
        }

        [Fact]
        public void PawnNeed_IsModerateRisk_ContainsWriteOp()
        {
            Assert.Equal(MechanismRisk.Moderate, _h2Mechanisms[12].Risk);
            Assert.Contains(MechanismOperationType.Set, _h2Mechanisms[12].SupportedOperations);
        }

        // ─── map.wealth ─────────────────────────────────────────
        [Fact]
        public void MapWealth_RegistersOnlyQueryTool()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[13]));
            Assert.NotNull(tools.FindById("map.wealth.query"));
            Assert.Null(tools.FindById("map.wealth.set"));
        }

        [Fact]
        public void MapWealth_QueryTool_PawnIdIsOptional()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[13]));
            var handler = tools.FindById("map.wealth.query");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.DoesNotContain("pawn_id", schema["required"]?.Values<string>() ?? Enumerable.Empty<string>());
        }

        // ─── world.faction ──────────────────────────────────────
        [Fact]
        public void WorldFaction_RegistersQuerySetListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[14]));
            Assert.NotNull(tools.FindById("world.faction.query"));
            Assert.NotNull(tools.FindById("world.faction.set"));
            Assert.NotNull(tools.FindById("world.faction.list"));
        }

        [Fact]
        public void WorldFaction_IsDangerousRisk()
        {
            Assert.Equal(MechanismRisk.Dangerous, _h2Mechanisms[14].Risk);
        }

        // ─── world.storyteller ──────────────────────────────────
        [Fact]
        public void WorldStoryteller_RegistersQueryTriggerListTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[15]));
            Assert.NotNull(tools.FindById("world.storyteller.query"));
            Assert.NotNull(tools.FindById("world.storyteller.trigger"));
            Assert.NotNull(tools.FindById("world.storyteller.list"));
        }

        [Fact]
        public void WorldStoryteller_IsDangerousRisk()
        {
            Assert.Equal(MechanismRisk.Dangerous, _h2Mechanisms[15].Risk);
        }

        // ─── world.choice_letter ────────────────────────────────
        [Fact]
        public void WorldChoiceLetter_RegistersQueryAndTriggerTools()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[16]));
            Assert.NotNull(tools.FindById("world.choice_letter.query"));
            Assert.NotNull(tools.FindById("world.choice_letter.trigger"));
        }

        [Fact]
        public void WorldChoiceLetter_TriggerTool_PawnIdNotRequired()
        {
            var (tools, mechs) = CreateRegistries();
            mechs.Register(new PerMechanismStub(_h2Mechanisms[16]));
            var handler = tools.FindById("world.choice_letter.trigger");
            Assert.NotNull(handler);
            var schema = JObject.Parse(handler.Definition.ParametersSchema);
            Assert.DoesNotContain("pawn_id", schema["required"]?.Values<string>() ?? Enumerable.Empty<string>());
        }

        // ─── Cross-mechanism consistency tests ──────────────────
        [Fact]
        public void AllDangerousMechanisms_HaveWriteOperations()
        {
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };
            var dangerous = _h2Mechanisms.Where(m => m.Risk == MechanismRisk.Dangerous);
            foreach (var m in dangerous)
            {
                Assert.True(m.SupportedOperations.Any(op => writeOps.Contains(op)),
                    $"Dangerous mechanism '{m.MechanismId}' must have at least one write operation");
            }
        }

        [Fact]
        public void AllSafeMechanisms_HaveOnlyReadOperations()
        {
            var writeOps = new HashSet<MechanismOperationType>
            {
                MechanismOperationType.Set, MechanismOperationType.Add,
                MechanismOperationType.Remove, MechanismOperationType.Toggle,
                MechanismOperationType.Trigger
            };
            var safe = _h2Mechanisms.Where(m => m.Risk == MechanismRisk.Safe);
            foreach (var m in safe)
            {
                Assert.All(m.SupportedOperations, op => Assert.DoesNotContain(op, writeOps));
            }
        }

        [Fact]
        public void AllPawnMechanisms_QueryTool_HasPawnIdRequired()
        {
            var (tools, mechs) = CreateRegistries();
            RegisterAll(mechs);
            var pawnQuery = _h2Mechanisms.Where(m => m.Scope == MechanismScope.Pawn && m.SupportedOperations.Contains(MechanismOperationType.Query));
            foreach (var contract in pawnQuery)
            {
                var handler = tools.FindById($"{contract.MechanismId}.query");
                Assert.NotNull(handler);
                var schema = JObject.Parse(handler.Definition.ParametersSchema);
                Assert.Contains("pawn_id", schema["required"]!.Values<string>());
            }
        }

        [Fact]
        public void AllWorldMechanisms_QueryTool_PawnIdNotRequired()
        {
            var (tools, mechs) = CreateRegistries();
            RegisterAll(mechs);
            var worldQuery = _h2Mechanisms.Where(m => m.Scope == MechanismScope.World && m.SupportedOperations.Contains(MechanismOperationType.Query));
            foreach (var contract in worldQuery)
            {
                var handler = tools.FindById($"{contract.MechanismId}.query");
                Assert.NotNull(handler);
                var schema = JObject.Parse(handler.Definition.ParametersSchema);
                Assert.DoesNotContain("pawn_id", schema["required"]?.Values<string>() ?? Enumerable.Empty<string>());
            }
        }

        [Fact]
        public void AllMechanismsWithList_ListTool_HasCategoryFilter()
        {
            var (tools, mechs) = CreateRegistries();
            RegisterAll(mechs);
            var withList = _h2Mechanisms.Where(m => m.SupportedOperations.Contains(MechanismOperationType.List));
            foreach (var contract in withList)
            {
                var handler = tools.FindById($"{contract.MechanismId}.list");
                Assert.NotNull(handler);
                var schema = JObject.Parse(handler.Definition.ParametersSchema);
                Assert.NotNull(schema["properties"]!["category"]);
            }
        }

        private class PerMechanismStub : IGameMechanism
        {
            string IExtension.Id => MechanismId;
            public string MechanismId { get; }
            public MechanismScope Scope { get; }
            public MechanismRisk Risk { get; }
            public IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
            public MechanismDocs Docs { get; }
            private readonly IReadOnlyList<MechanismActionInfo>? _writeActions;

            public PerMechanismStub(MechanismContract contract, IReadOnlyList<MechanismActionInfo>? writeActions = null)
            {
                MechanismId = contract.MechanismId;
                Scope = contract.Scope;
                Risk = contract.Risk;
                SupportedOperations = contract.SupportedOperations;
                Docs = new MechanismDocs { Summary = $"{contract.MechanismId} test stub" };
                _writeActions = writeActions;
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
            public IReadOnlyList<MechanismActionInfo>? GetWriteActions() => _writeActions;
            public MechanismRisk GetRiskForOperation(MechanismOperationType operation) => Risk;
        }
    }
}
