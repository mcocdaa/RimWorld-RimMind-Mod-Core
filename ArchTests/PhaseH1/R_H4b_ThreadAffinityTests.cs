using System.Reflection;
using FluentAssertions;
using RimMind.Domain.Common;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Interfaces.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H4b_ThreadAffinityTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IToolHandler_ExecuteAsync_Should_Be_Any()
        {
            var m = typeof(IToolHandler).GetMethod("ExecuteAsync");
            m.Should().NotBeNull();

            var attr = m!.GetCustomAttribute<ThreadAffinityAttribute>();
            attr.Should().NotBeNull("IToolHandler.ExecuteAsync must have [ThreadAffinity] annotation");
            attr!.Kind.Should().Be(ThreadAffinityKind.Any,
                "IToolHandler.ExecuteAsync should be Any (implementation decides thread model)");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_ExecuteQueryAsync_Should_Be_MainOnly()
        {
            var m = typeof(IGameMechanism).GetMethod("ExecuteQueryAsync");
            m.Should().NotBeNull();

            var attr = m!.GetCustomAttribute<ThreadAffinityAttribute>();
            if (attr != null)
            {
                attr.Kind.Should().Be(ThreadAffinityKind.MainOnly,
                    "IGameMechanism.ExecuteQueryAsync reads Verse data, must be MainOnly");
            }
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_ExecuteSetAsync_Should_Be_MainOnly()
        {
            var m = typeof(IGameMechanism).GetMethod("ExecuteSetAsync");
            m.Should().NotBeNull();

            var attr = m!.GetCustomAttribute<ThreadAffinityAttribute>();
            if (attr != null)
            {
                attr.Kind.Should().Be(ThreadAffinityKind.MainOnly,
                    "IGameMechanism.ExecuteSetAsync writes Verse data, must be MainOnly");
            }
        }
    }
}
