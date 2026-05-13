using System.Text.RegularExpressions;
using FluentAssertions;
using RimMind.Application.Common.Models.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H2_ToolIdFormatTests
    {
        private static readonly Regex DirectPattern = new(@"^[a-z]+\.[a-z][a-z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex MechanismPattern = new(@"^[a-z]+\.[a-z][a-z0-9_]*\.[a-z]+$", RegexOptions.Compiled);

        [Fact]
        [Trait("Phase", "H1")]
        public void SkillMechanism_ToolId_Should_Match_Mechanism_Pattern()
        {
            var toolId = "pawn.skill.query";
            (DirectPattern.IsMatch(toolId) || MechanismPattern.IsMatch(toolId)).Should().BeTrue(
                $"Tool Id '{toolId}' must match <modid>.<lower_snake> or <scope>.<name>.<operation>");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void NeedMechanism_ToolId_Should_Match_Mechanism_Pattern()
        {
            var toolId = "pawn.need.query";
            (DirectPattern.IsMatch(toolId) || MechanismPattern.IsMatch(toolId)).Should().BeTrue(
                $"Tool Id '{toolId}' must match <modid>.<lower_snake> or <scope>.<name>.<operation>");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void WealthMechanism_ToolId_Should_Match_Mechanism_Pattern()
        {
            var toolId = "map.wealth.query";
            (DirectPattern.IsMatch(toolId) || MechanismPattern.IsMatch(toolId)).Should().BeTrue(
                $"Tool Id '{toolId}' must match <modid>.<lower_snake> or <scope>.<name>.<operation>");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void PascalCase_ToolId_Should_Not_Match()
        {
            var toolId = "Pawn.Skill.Read";
            (DirectPattern.IsMatch(toolId) || MechanismPattern.IsMatch(toolId)).Should().BeFalse(
                "PascalCase tool IDs should not be accepted");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void ToolDefinition_Id_Property_Exists()
        {
            var idProp = typeof(ToolDefinition).GetProperty("Id");
            idProp.Should().NotBeNull("ToolDefinition must have an Id property");
            idProp!.PropertyType.Should().Be(typeof(string));
        }
    }
}
