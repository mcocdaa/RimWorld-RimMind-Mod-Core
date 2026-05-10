using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using RimMind.Contracts.Client;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseJ
{
    public class NoLegacySuccessFieldTests
    {
        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_AIResponse_MustNotContain_Success_PropertyOrField()
        {
            var type = typeof(AIResponse);

            var property = type.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
            property.Should().BeNull("AIResponse must not have a Success property — use Result<T,E>.IsOk instead");

            var field = type.GetField("Success", BindingFlags.Public | BindingFlags.Instance);
            field.Should().BeNull("AIResponse must not have a Success field — use Result<T,E>.IsOk instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_AIResponse_MustNotContain_Error_PropertyOrField()
        {
            var type = typeof(AIResponse);

            var property = type.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance);
            property.Should().BeNull("AIResponse must not have an Error property — use Result<T,E>.IsErr instead");

            var field = type.GetField("Error", BindingFlags.Public | BindingFlags.Instance);
            field.Should().BeNull("AIResponse must not have an Error field — use Result<T,E>.IsErr instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_AIResponse_MustNotContain_Failure_Method()
        {
            var type = typeof(AIResponse);

            var method = type.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            method.Should().BeNull("AIResponse must not have a Failure method — use Result<T,E>.Err() instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_AIResponse_MustNotContain_Cancelled_Method()
        {
            var type = typeof(AIResponse);

            var method = type.GetMethod("Cancelled", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            method.Should().BeNull("AIResponse must not have a Cancelled method — use Result<T,E>.Err(RimMindErrors.Cancelled()) instead");
        }
    }
}
