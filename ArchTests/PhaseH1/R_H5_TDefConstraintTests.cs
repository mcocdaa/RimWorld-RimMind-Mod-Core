using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Infrastructure.Mechanisms;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H5_TDefConstraintTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Exist_In_Infrastructure_Mechanisms()
        {
            typeof(GameMechanismBaseNoDef).Namespace.Should().StartWith("RimMind.Infrastructure.Mechanisms",
                "GameMechanismBaseNoDef must be in RimMind.Infrastructure.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Implement_IGameMechanism()
        {
            typeof(IGameMechanism).IsAssignableFrom(typeof(GameMechanismBaseNoDef)).Should().BeTrue(
                "GameMechanismBaseNoDef must implement IGameMechanism");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBase_TDef_Should_Be_Constrained_To_Def()
        {
            var constraints = ReadGenericConstraintClause();

            Regex.IsMatch(constraints, @"(^|,)\s*Def\s*(,|$)").Should().BeTrue(
                "TDef must be constrained to Verse.Def so DefDatabase<TDef> works");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBase_TDef_Should_Have_New_Constraint()
        {
            var constraints = ReadGenericConstraintClause();

            Regex.IsMatch(constraints, @"(^|,)\s*new\s*\(\s*\)\s*(,|$)").Should().BeTrue(
                "TDef must have 'new()' constraint for DefDatabase<TDef>.GetNamed");
        }

        private static string ReadGenericConstraintClause()
        {
            var sourceFile = Path.Combine(
                ArchTestExtensions.FindSourceDirectory(),
                "Infrastructure",
                "Mechanisms",
                "GameMechanismBase.cs");
            File.Exists(sourceFile).Should().BeTrue("GameMechanismBase.cs must exist for source-level analysis");

            var source = File.ReadAllText(sourceFile);
            var match = Regex.Match(
                source,
                @"class\s+GameMechanismBase\s*<\s*TDef\s*>[^\{]*?where\s+TDef\s*:\s*(?<constraints>[^\r\n\{]+)",
                RegexOptions.Singleline);
            match.Success.Should().BeTrue("GameMechanismBase<TDef> must declare an explicit generic constraint clause");
            return match.Groups["constraints"].Value;
        }
    }
}
