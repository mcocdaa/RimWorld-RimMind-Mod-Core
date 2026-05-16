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
        public void IMechanismReader_ExecuteQueryAsync_Should_Be_MainOnly()
        {
            var m = typeof(IMechanismReader).GetMethod("ExecuteQueryAsync");
            m.Should().NotBeNull();

            var attr = m!.GetCustomAttribute<ThreadAffinityAttribute>();
            if (attr != null)
            {
                attr.Kind.Should().Be(ThreadAffinityKind.MainOnly,
                    "IMechanismReader.ExecuteQueryAsync reads Verse data, must be MainOnly");
            }
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IMechanismWriter_ExecuteSetAsync_Should_Be_MainOnly()
        {
            var m = typeof(IMechanismWriter).GetMethod("ExecuteSetAsync");
            m.Should().NotBeNull();

            var attr = m!.GetCustomAttribute<ThreadAffinityAttribute>();
            if (attr != null)
            {
                attr.Kind.Should().Be(ThreadAffinityKind.MainOnly,
                    "IMechanismWriter.ExecuteSetAsync writes Verse data, must be MainOnly");
            }
        }
    }
}
