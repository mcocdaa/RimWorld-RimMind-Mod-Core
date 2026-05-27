using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseJ
{
    /// <summary>
    /// R_J1: Updated for K-phase — IAIClient.SendAsync returns Result&lt;LlmResponse, RimMindError&gt;.
    /// </summary>
    public class ApiReturnsResultTests
    {
        [Fact]
        [Trait("Phase", "J")]
        public void R_J1_IAIClient_SendAsync_ShouldReturn_Task_Result_LlmResponse_RimMindError()
        {
            var aiClientType = typeof(IAIClient);
            var sendAsyncMethod = aiClientType.GetMethod("SendAsync", new[] { typeof(LlmRequestEnvelope) });

            sendAsyncMethod.Should().NotBeNull("IAIClient must define SendAsync(LlmRequestEnvelope)");

            var returnType = sendAsyncMethod!.ReturnType;
            returnType.Should().Be(typeof(Task<Result<LlmResponse, RimMindError>>),
                "IAIClient.SendAsync must return Task<Result<LlmResponse, RimMindError>>");
        }
    }

    /// <summary>
    /// R_J2: Updated for K-phase — LlmResponse has no Success/Error/Failure/Cancelled members.
    /// </summary>
    public class NoLegacySuccessFieldTests
    {
        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_LlmResponse_MustNotContain_Success_PropertyOrField()
        {
            var type = typeof(LlmResponse);

            var property = type.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
            property.Should().BeNull("LlmResponse must not have a Success property — use Result<T,E>.IsOk instead");

            var field = type.GetField("Success", BindingFlags.Public | BindingFlags.Instance);
            field.Should().BeNull("LlmResponse must not have a Success field — use Result<T,E>.IsOk instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_LlmResponse_MustNotContain_Error_PropertyOrField()
        {
            var type = typeof(LlmResponse);

            var property = type.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance);
            property.Should().BeNull("LlmResponse must not have an Error property — use Result<T,E>.IsErr instead");

            var field = type.GetField("Error", BindingFlags.Public | BindingFlags.Instance);
            field.Should().BeNull("LlmResponse must not have an Error field — use Result<T,E>.IsErr instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_LlmResponse_MustNotContain_Failure_Method()
        {
            var type = typeof(LlmResponse);

            var method = type.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            method.Should().BeNull("LlmResponse must not have a Failure method — use Result<T,E>.Err() instead");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J2_LlmResponse_MustNotContain_Cancelled_Method()
        {
            var type = typeof(LlmResponse);

            var method = type.GetMethod("Cancelled", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            method.Should().BeNull("LlmResponse must not have a Cancelled method — use Result<T,E>.Err(RimMindErrors.Cancelled()) instead");
        }
    }
}
