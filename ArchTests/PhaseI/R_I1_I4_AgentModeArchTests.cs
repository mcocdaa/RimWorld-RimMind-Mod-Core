using System;
using System.Linq;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Domain.Agent.Modes;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseI
{
    public class R_I1_AgentModeNamespaceTests
    {
        [Fact]
        [Trait("Phase", "I")]
        public void IAgentMode_Interface_Should_Be_In_AgentModes_Namespace()
        {
            typeof(IAgentMode).Namespace.Should().Be("RimMind.Application.Common.Interfaces.Agent.Modes",
                "IAgentMode must be in Application.Common.Interfaces.Agent.Modes namespace (R-I1)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void IThinkStrategy_Interface_Should_Be_In_AgentModes_Namespace()
        {
            typeof(IThinkStrategy).Namespace.Should().Be("RimMind.Application.Common.Interfaces.Agent.Modes",
                "IThinkStrategy must be in Application.Common.Interfaces.Agent.Modes namespace (R-I1)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void AgentModeId_Should_Be_In_Domain_Agent_Modes_Namespace()
        {
            typeof(AgentModeId).Namespace.Should().Be("RimMind.Domain.Agent.Modes",
                "AgentModeId must be in Domain.Agent.Modes namespace");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void AgentDecision_Should_Be_In_Domain_Agent_Modes_Namespace()
        {
            typeof(AgentDecision).Namespace.Should().Be("RimMind.Domain.Agent.Modes",
                "AgentDecision must be in Domain.Agent.Modes namespace");
        }
    }

    public class R_I2_AgentModeIdFormatTests
    {
        [Theory]
        [Trait("Phase", "I")]
        [InlineData("reactive", true)]
        [InlineData("proactive", true)]
        [InlineData("my_custom_mode", true)]
        [InlineData("Reactive", false)]
        [InlineData("my-custom", false)]
        [InlineData("", false)]
        public void AgentModeId_Value_Should_Match_LowerSnakeCase(string value, bool expectedValid)
        {
            var isValid = System.Text.RegularExpressions.Regex.IsMatch(value, @"^[a-z][a-z0-9_]*$");
            isValid.Should().Be(expectedValid, $"AgentModeId.Value '{value}' should {(expectedValid ? "" : "not ")}match lower_snake_case (R-I2)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void Builtin_ModeIds_Should_Be_LowerSnakeCase()
        {
            AgentModeId.Reactive.Value.Should().MatchRegex(@"^[a-z][a-z0-9_]*$",
                "AgentModeId.Reactive must be lower_snake_case (R-I2)");
            AgentModeId.Proactive.Value.Should().MatchRegex(@"^[a-z][a-z0-9_]*$",
                "AgentModeId.Proactive must be lower_snake_case (R-I2)");
        }
    }

    public class R_I3_ParseDecisionReturnTypeTests
    {
        [Fact]
        [Trait("Phase", "I")]
        public void IThinkStrategy_Should_Have_ParseDecision_Method()
        {
            var method = typeof(IThinkStrategy).GetMethod("ParseDecision");
            method.Should().NotBeNull("IThinkStrategy must have ParseDecision method (R-I3)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void AgentDecision_Should_Be_Sealed_Record()
        {
            typeof(AgentDecision).IsSealed.Should().BeTrue("AgentDecision must be sealed record (R-I3)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void Result_Type_Should_Exist_With_Correct_Generic_Args()
        {
            var resultType = typeof(Result<,>);
            resultType.Should().NotBeNull("Result<,> generic type must exist (R-I3)");

            var concreteResult = typeof(Result<AgentDecision, RimMindError>);
            concreteResult.Should().NotBeNull("Result<AgentDecision, RimMindError> must be constructible (R-I3)");
        }
    }

    public class R_I4_PawnThinkerNoDirectConstructionTests
    {
        [Fact]
        [Trait("Phase", "I")]
        public void IThinkStrategy_Should_Have_BuildRequest_Method()
        {
            var method = typeof(IThinkStrategy).GetMethod("BuildRequest");
            method.Should().NotBeNull("IThinkStrategy must have BuildRequest method (R-I4)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void IAgentMode_Should_Have_GetThinkStrategy_Method()
        {
            var method = typeof(IAgentMode).GetMethod("GetThinkStrategy");
            method.Should().NotBeNull("IAgentMode must have GetThinkStrategy method (R-I4)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void IAgentMode_Should_Have_ShouldThink_Method()
        {
            var method = typeof(IAgentMode).GetMethod("ShouldThink");
            method.Should().NotBeNull("IAgentMode must have ShouldThink method (R-I4)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void IAgentMode_Should_Have_AllowedToolIds_Method()
        {
            var method = typeof(IAgentMode).GetMethod("AllowedToolIds");
            method.Should().NotBeNull("IAgentMode must have AllowedToolIds method (R-I4)");
        }

        [Fact]
        [Trait("Phase", "I")]
        public void IAgentMode_Should_Inherit_IExtension()
        {
            typeof(IExtension).IsAssignableFrom(typeof(IAgentMode)).Should().BeTrue(
                "IAgentMode must inherit IExtension (R-I4)");
        }
    }
}
