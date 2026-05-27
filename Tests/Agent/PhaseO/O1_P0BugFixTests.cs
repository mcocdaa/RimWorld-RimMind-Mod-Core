using System.Reflection;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Models;
using RimMind.Application.Features.Agent;
using RimMind.Domain.Agent.Modes;
using Xunit;

namespace RimMind.Tests.Agent.PhaseO
{
    /// <summary>
    /// ArchTest-style tests verifying O1 P0 BUG fixes.
    /// Presentation layer (PawnThinker, MechanismActionExecutor) depends on Verse,
    /// so we verify structural properties via reflection on Application/Domain contracts.
    /// </summary>
    public class O1_P0BugFixTests
    {
        // O1.1: IActionExecutor.ExecuteDecision accepts pawnId parameter
        // (MechanismActionExecutor is in Infrastructure which is not compiled into test project,
        //  so we verify the contract interface that the fix must conform to)
        [Fact]
        public void IActionExecutor_ExecuteDecision_AcceptsPawnIdParameter()
        {
            var method = typeof(IActionExecutor).GetMethod("ExecuteDecision");
            Assert.NotNull(method);
            var parameters = method.GetParameters();
            // Second parameter should be int pawnId (not hardcoded 0)
            Assert.True(parameters.Length >= 2);
            Assert.Equal("pawnId", parameters[1].Name);
            Assert.Equal(typeof(int), parameters[1].ParameterType);
        }

        [Fact]
        public void DecisionMapper_ToWriteArgs_AcceptsPawnIdParameter()
        {
            var method = typeof(DecisionMapper).GetMethod("ToWriteArgs");
            Assert.NotNull(method);
            var parameters = method.GetParameters();
            // Second parameter should be int pawnId
            Assert.True(parameters.Length >= 2);
            Assert.Equal("pawnId", parameters[1].Name);
            Assert.Equal(typeof(int), parameters[1].ParameterType);
        }

        // O1.2: ThinkRequestTimeoutTicks exists and is used
        [Fact]
        public void RimMindDefaults_ThinkRequestTimeoutTicks_IsPositive()
        {
            var field = typeof(RimMindDefaults).GetField("ThinkRequestTimeoutTicks",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            var value = (int)field.GetValue(null)!;
            Assert.True(value > 0, "ThinkRequestTimeoutTicks should be positive");
            Assert.Equal(1800, value);
        }

        // O1.3: IPawnThinker has new interface members
        [Fact]
        public void IPawnThinker_Defines_ShouldThink()
        {
            var method = typeof(IPawnThinker).GetMethod("ShouldThink");
            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method.ReturnType);
        }

        [Fact]
        public void IPawnThinker_Defines_ResetThinking()
        {
            var method = typeof(IPawnThinker).GetMethod("ResetThinking");
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method.ReturnType);
        }

        [Fact]
        public void IPawnThinker_Defines_IsThinking()
        {
            var property = typeof(IPawnThinker).GetProperty("IsThinking");
            Assert.NotNull(property);
            Assert.Equal(typeof(bool), property.PropertyType);
            Assert.True(property.CanRead);
        }

        // O1.3: DecisionMapper correctly passes pawnId through
        [Fact]
        public void DecisionMapper_ToWriteArgs_UsesPawnId()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "test");
            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 42);
            Assert.Equal(42, args.PawnId);
        }

        [Fact]
        public void DecisionMapper_ToWriteArgs_PawnIdZero_WhenExplicitlyZero()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "test");
            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 0);
            Assert.Equal(0, args.PawnId);
        }

        [Fact]
        public void DecisionMapper_ToWriteArgs_PawnIdPreserved()
        {
            var decision = new AgentDecision(
                ActionIntent: "pawn.job.force_rest",
                Reason: "test");
            var args = DecisionMapper.ToWriteArgs(decision, pawnId: 999);
            Assert.Equal(999, args.PawnId);
        }
    }
}
