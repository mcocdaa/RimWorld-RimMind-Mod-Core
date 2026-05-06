using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.General
{
    public class InternalNotLeakingTests
    {
        private static readonly string[] AllowedTestAssemblies = new[]
        {
            "RimMindCore.Tests",
            "RimMindCore.ArchTests"
        };

        [Fact]
        [Trait("Phase", "General")]
        public void R_G2_InternalsVisibleTo_ShouldOnlyTarget_TestProjects()
        {
            var assembly = typeof(InternalNotLeakingTests).Assembly;
            var ivtAttributes = assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
                .ToList();

            var violating = ivtAttributes
                .Where(a => !AllowedTestAssemblies.Contains(a.AssemblyName))
                .ToList();

            violating.Should().BeEmpty(
                $"InternalsVisibleTo must only target test projects. " +
                $"Violating assemblies: {string.Join(", ", violating.Select(a => a.AssemblyName))}");
        }
    }
}
