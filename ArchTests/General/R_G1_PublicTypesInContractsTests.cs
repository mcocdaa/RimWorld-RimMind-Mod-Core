using System.Linq;
using FluentAssertions;
using NetArchTest.Rules;
using RimMind.Core.ArchTests;
using Xunit;

namespace RimMind.Core.ArchTests.General
{
    public class PublicTypesInContractsTests
    {
        [Fact]
        [Trait("Phase", "General")]
        public void R_G1_RimMindPublicTypes_ShouldResideIn_ValidNamespaces()
        {
            var validPrefixes = new[]
            {
                "RimMind.Core",
                "RimMind.Contracts"
            };

            var result = Types.InAssembly(typeof(PublicTypesInContractsTests).Assembly)
                .That()
                .ResideInNamespaceStartingWith("RimMind")
                .And()
                .ArePublic()
                .And()
                .AreNotInterfaces()
                .Should()
                .ResideInNamespaceStartingWith("RimMind.Core")
                .Or()
                .ResideInNamespaceStartingWith("RimMind.Contracts")
                .Or()
                .ResideInNamespaceStartingWith("RimMind.Kernel")
                .Or()
                .ResideInNamespaceStartingWith("RimMind.Adapters")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"All RimMind public types must reside in RimMind.Core.*, RimMind.Contracts.*, RimMind.Kernel.*, or RimMind.Adapters.* namespaces. Violating types:\n  {result.FormatFailingTypes()}");
        }
    }
}
