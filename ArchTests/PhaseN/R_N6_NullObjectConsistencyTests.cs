using System;
using System.Reflection;
using FluentAssertions;
using RimMind.Application.Common.Defaults;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN;

public sealed class R_N6_NullObjectConsistencyTests
{
    [Theory]
    [InlineData(typeof(NullAgentActionBridge))]
    [InlineData(typeof(NullDialogueTrigger))]
    [InlineData(typeof(NullIncidentExecutedListener))]
    [InlineData(typeof(NullModCooldown))]
    [InlineData(typeof(NullSkipCheck))]
    [Trait("Phase", "N")]
    public void R_N6_NullClasses_Should_Expose_Instance_Singleton(Type type)
    {
        var instanceField = type.GetField("Instance",
            BindingFlags.Public | BindingFlags.Static);

        instanceField.Should().NotBeNull(
            "{0} should expose a public static Instance field for the singleton Null-Object pattern.", type.Name);
        instanceField!.IsInitOnly.Should().BeTrue(
            "{0}.Instance should be readonly to lock the singleton identity.", type.Name);

        var value = instanceField.GetValue(null);
        value.Should().NotBeNull(
            "{0}.Instance should resolve to a non-null singleton instance.", type.Name);
        value!.GetType().Should().Be(type,
            "{0}.Instance should be assigned the same type to preserve Null-Object identity.", type.Name);
    }
}
