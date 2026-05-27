using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Modes;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Tests.Agent
{
    public class DecisionMapperTests
    {
        [Fact]
        public void ParseActionIntent_DotNotation_ReturnsMechanismIdAndAction()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent("pawn.job.force_rest");
            Assert.Equal("pawn.job", mechanismId);
            Assert.Equal("force_rest", action);
        }

        [Fact]
        public void ParseActionIntent_SingleWord_DefaultsToPawnJobMechanism()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent("force_rest");
            Assert.Equal("pawn.job", mechanismId);
            Assert.Equal("force_rest", action);
        }

        [Fact]
        public void ParseActionIntent_EmptyString_ReturnsDefaults()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent("");
            Assert.Equal("pawn.job", mechanismId);
            Assert.Equal("", action);
        }

        [Fact]
        public void ParseActionIntent_Null_ReturnsDefaults()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent(null!);
            Assert.Equal("pawn.job", mechanismId);
            Assert.Equal("", action);
        }

        [Fact]
        public void ParseActionIntent_MultipleDots_SplitsOnLastDot()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent("pawn.interaction.social_chat");
            Assert.Equal("pawn.interaction", mechanismId);
            Assert.Equal("social_chat", action);
        }

        [Fact]
        public void ParseActionIntent_LeadingDot_TreatsAsSingleWord()
        {
            var (mechanismId, action) = DecisionMapper.ParseActionIntent(".force_rest");
            Assert.Equal("pawn.job", mechanismId);
            Assert.Equal(".force_rest", action);
        }

        [Fact]
        public void ToWriteArgs_MapsBasicFields()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "need rest",
                TargetPawnId: null,
                Param: null);

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);

            Assert.Equal("pawn.job", args.MechanismId);
            Assert.Equal(42, args.PawnId);
            Assert.Equal("force_rest", args.Action);
            Assert.Null(args.ValueJson);
            Assert.Null(args.Params);
        }

        [Fact]
        public void ToWriteArgs_MapsTargetPawnId()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.tend_pawn",
                Reason: "",
                TargetPawnId: "99",
                Param: null);

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);

            Assert.NotNull(args.Params);
            Assert.True(args.Params.ContainsKey("target_pawn_id"));
            Assert.Equal("99", args.Params["target_pawn_id"]);
        }

        [Fact]
        public void ToWriteArgs_ParsesJsonObjectParam()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.move_to",
                Reason: "",
                TargetPawnId: null,
                Param: "{\"cell_x\": 10, \"cell_z\": 20}");

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);

            Assert.NotNull(args.Params);
            Assert.Equal("10", args.Params["cell_x"]);
            Assert.Equal("20", args.Params["cell_z"]);
            Assert.NotNull(args.ValueJson);
        }

        [Fact]
        public void ToWriteArgs_ParsesNonJsonParam_AsValueJson()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "",
                TargetPawnId: null,
                Param: "simple_value");

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);

            Assert.Equal("simple_value", args.ValueJson);
            Assert.Null(args.Params);
        }

        [Fact]
        public void ToWriteArgs_NullParam_NoValueNoParams()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "",
                TargetPawnId: null,
                Param: null);

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 1);

            Assert.Null(args.ValueJson);
            Assert.Null(args.Params);
        }

        [Fact]
        public void ToWriteArgs_TargetPawnIdAndJsonParam_MergesBoth()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.tend_pawn",
                Reason: "",
                TargetPawnId: "55",
                Param: "{\"urgency\": \"high\"}");

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);

            Assert.NotNull(args.Params);
            Assert.Equal("55", args.Params["target_pawn_id"]);
            Assert.Equal("high", args.Params["urgency"]);
        }

        [Fact]
        public void ParseParam_InvalidJson_TreatsAsValueJson()
        {
            var (valueJson, paramsDict) = DecisionMapper.ParseParam("{invalid json");
            Assert.Equal("{invalid json", valueJson);
            Assert.Null(paramsDict);
        }

        [Fact]
        public void ParseParam_JsonArray_TreatsAsValueJson()
        {
            var (valueJson, paramsDict) = DecisionMapper.ParseParam("[1, 2, 3]");
            Assert.Equal("[1, 2, 3]", valueJson);
            Assert.Null(paramsDict);
        }

        [Fact]
        public void ParseParam_EmptyString_ReturnsNulls()
        {
            var (valueJson, paramsDict) = DecisionMapper.ParseParam("");
            Assert.Null(valueJson);
            Assert.Null(paramsDict);
        }

        [Fact]
        public void ToWriteArgs_ToolCallId_MapsToTraceId()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "",
                TargetPawnId: null,
                Param: null,
                ToolCallId: "call_abc123");

            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 1);

            Assert.Equal("call_abc123", args.TraceId);
        }

        // --- InferOperationType tests ---

        [Fact]
        public void InferOperationType_Null_ReturnsSet()
        {
            Assert.Equal(MechanismOperationType.Set, DecisionMapper.InferOperationType(null!));
        }

        [Fact]
        public void InferOperationType_EmptyString_ReturnsSet()
        {
            Assert.Equal(MechanismOperationType.Set, DecisionMapper.InferOperationType(""));
        }

        [Theory]
        [InlineData("force_rest")]
        [InlineData("trigger_alarm")]
        [InlineData("emergency_evacuate")]
        public void InferOperationType_TriggerPrefixes_ReturnTrigger(string action)
        {
            Assert.Equal(MechanismOperationType.Trigger, DecisionMapper.InferOperationType(action));
        }

        [Theory]
        [InlineData("set_priority")]
        [InlineData("adjust_schedule")]
        [InlineData("configure_settings")]
        public void InferOperationType_SetPrefixes_ReturnSet(string action)
        {
            Assert.Equal(MechanismOperationType.Set, DecisionMapper.InferOperationType(action));
        }

        [Theory]
        [InlineData("add_resource")]
        [InlineData("grant_permission")]
        [InlineData("give_item")]
        public void InferOperationType_AddPrefixes_ReturnAdd(string action)
        {
            Assert.Equal(MechanismOperationType.Add, DecisionMapper.InferOperationType(action));
        }

        [Theory]
        [InlineData("toggle_power")]
        [InlineData("switch_mode")]
        public void InferOperationType_TogglePrefixes_ReturnToggle(string action)
        {
            Assert.Equal(MechanismOperationType.Toggle, DecisionMapper.InferOperationType(action));
        }

        [Theory]
        [InlineData("remove_zone")]
        [InlineData("revoke_access")]
        [InlineData("clear_cache")]
        public void InferOperationType_RemovePrefixes_ReturnRemove(string action)
        {
            Assert.Equal(MechanismOperationType.Remove, DecisionMapper.InferOperationType(action));
        }

        [Fact]
        public void InferOperationType_UnknownPrefix_DefaultsToSet()
        {
            Assert.Equal(MechanismOperationType.Set, DecisionMapper.InferOperationType("wander_around"));
        }
    }
}
