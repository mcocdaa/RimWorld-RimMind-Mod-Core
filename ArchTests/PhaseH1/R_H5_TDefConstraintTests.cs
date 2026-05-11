using System.Reflection;
using FluentAssertions;
using RimMind.Contracts.Mechanisms;
using RimMind.Kernel.Mechanisms;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H5_TDefConstraintTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Exist_In_Kernel_Mechanisms()
        {
            typeof(GameMechanismBaseNoDef).Namespace.Should().StartWith("RimMind.Kernel.Mechanisms",
                "GameMechanismBaseNoDef must be in RimMind.Kernel.Mechanisms namespace");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void GameMechanismBaseNoDef_Should_Implement_IGameMechanism()
        {
            typeof(IGameMechanism).IsAssignableFrom(typeof(GameMechanismBaseNoDef)).Should().BeTrue(
                "GameMechanismBaseNoDef must implement IGameMechanism");
        }

        [Fact(Skip = "Requires RimWorld runtime (Assembly-CSharp); verified by source inspection")]
        [Trait("Phase", "H1")]
        public void GameMechanismBase_TDef_Should_Be_Constrained_To_Def()
        {
            var tdef = typeof(GameMechanismBase<>).GetGenericArguments()[0];
            var constraints = tdef.GetGenericParameterConstraints();

            constraints.Should().Contain(t => t.Name == "Def",
                "TDef must be constrained to Verse.Def so DefDatabase<TDef> works");
        }

        [Fact(Skip = "Requires RimWorld runtime (Assembly-CSharp); verified by source inspection")]
        [Trait("Phase", "H1")]
        public void GameMechanismBase_TDef_Should_Have_New_Constraint()
        {
            var tdef = typeof(GameMechanismBase<>).GetGenericArguments()[0];
            var attrs = tdef.GenericParameterAttributes;

            attrs.HasFlag(System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint).Should().BeTrue(
                "TDef must have 'new()' constraint for DefDatabase<TDef>.GetNamed");
        }
    }
}
