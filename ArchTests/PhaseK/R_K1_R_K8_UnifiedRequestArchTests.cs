using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseK
{
    /// <summary>
    /// R-K1: LlmRequestEnvelope is the sole request type — no AIRequest/ContextRequest in active code.
    /// </summary>
    public class UnifiedEnvelopeTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K1_LlmRequestEnvelope_Exists_In_Domain_Llm()
        {
            var type = typeof(LlmRequestEnvelope);
            type.Namespace.Should().Be("RimMind.Domain.Llm",
                "LlmRequestEnvelope must live in Domain.Llm");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K1_LlmRequestEnvelope_Has_Required_Fields()
        {
            var type = typeof(LlmRequestEnvelope);

            var requiredProperties = new[]
            {
                "RequestId", "NpcId", "Messages", "MaxTokens",
                "Temperature", "IsStreaming", "Priority", "ScenarioId"
            };

            foreach (var propName in requiredProperties)
            {
                var prop = type.GetProperty(propName);
                prop.Should().NotBeNull($"LlmRequestEnvelope must have property {propName}");
            }
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K1_LlmRequestEnvelopeBuilder_Exists()
        {
            var type = typeof(LlmRequestEnvelopeBuilder);
            type.Should().NotBeNull("LlmRequestEnvelopeBuilder must exist in Domain.Llm");

            var buildMethod = type.GetMethod("Build");
            buildMethod.Should().NotBeNull("LlmRequestEnvelopeBuilder must have a Build method");
            buildMethod!.ReturnType.Should().Be(typeof(LlmRequestEnvelope));
        }
    }

    /// <summary>
    /// R-K2: LlmResponse is the sole response type — no AIResponse in active code.
    /// </summary>
    public class UnifiedResponseTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K2_LlmResponse_Exists_In_Domain_Llm()
        {
            var type = typeof(LlmResponse);
            type.Namespace.Should().Be("RimMind.Domain.Llm",
                "LlmResponse must live in Domain.Llm");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K2_LlmResponse_Has_Required_Fields()
        {
            var type = typeof(LlmResponse);

            var requiredProperties = new[]
            {
                "Content", "State", "TokensUsed", "PromptTokens", "CompletionTokens"
            };

            foreach (var propName in requiredProperties)
            {
                var prop = type.GetProperty(propName);
                prop.Should().NotBeNull($"LlmResponse must have property {propName}");
            }
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K2_LlmChunk_Exists_For_Streaming()
        {
            var type = typeof(LlmChunk);
            type.Namespace.Should().Be("RimMind.Domain.Llm",
                "LlmChunk must live in Domain.Llm for streaming support");
        }
    }

    /// <summary>
    /// R-K3: Result&lt;T, RimMindError&gt; is the discriminated union error model.
    /// </summary>
    public class ResultErrorModelTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K3_Result_Has_IsOk_And_IsErr()
        {
            var resultType = typeof(Result<LlmResponse, RimMindError>);

            var isOkProp = resultType.GetProperty("IsOk");
            isOkProp.Should().NotBeNull("Result must have IsOk property");

            var isErrProp = resultType.GetProperty("IsErr");
            isErrProp.Should().NotBeNull("Result must have IsErr property");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K3_Result_Has_Ok_And_Err_Factories()
        {
            var resultType = typeof(Result<LlmResponse, RimMindError>);

            var okMethod = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);
            okMethod.Should().NotBeNull("Result must have Ok factory method");

            var errMethod = resultType.GetMethod("Err", BindingFlags.Public | BindingFlags.Static);
            errMethod.Should().NotBeNull("Result must have Err factory method");
        }
    }

    /// <summary>
    /// R-K4: RimMindAPI exposes only Send/SendAsync as entry points.
    /// </summary>
    public class ApiEntryPointsTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K4_RimMindAPI_Has_Send_Method()
        {
            var apiType = typeof(RimMind.Application.Api.RimMindAPI);
            var requestType = apiType.GetNestedType("Request");
            requestType.Should().NotBeNull("RimMindAPI must have nested Request type");

            var sendMethod = requestType!.GetMethod("Send", new[] { typeof(LlmRequestEnvelope), typeof(Action<Result<LlmResponse, RimMindError>>) });
            sendMethod.Should().NotBeNull("RimMindAPI.Request must have Send(LlmRequestEnvelope, Action<Result<LlmResponse, RimMindError>>) method");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K4_RimMindAPI_Has_SendAsync_Method()
        {
            var apiType = typeof(RimMind.Application.Api.RimMindAPI);
            var requestType = apiType.GetNestedType("Request");
            requestType.Should().NotBeNull("RimMindAPI must have nested Request type");

            var sendAsyncMethod = requestType!.GetMethod("SendAsync", new[] { typeof(LlmRequestEnvelope) });
            sendAsyncMethod.Should().NotBeNull("RimMindAPI.Request must have SendAsync(LlmRequestEnvelope) method");

            var returnType = sendAsyncMethod!.ReturnType;
            returnType.Should().Be(typeof(System.Threading.Tasks.Task<Result<LlmResponse, RimMindError>>),
                "SendAsync must return Task<Result<LlmResponse, RimMindError>>");
        }
    }

    /// <summary>
    /// R-K5: IAIClient uses LlmRequestEnvelope (not AIRequest).
    /// </summary>
    public class IAIClientSignatureTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K5_IAIClient_SendAsync_Accepts_LlmRequestEnvelope()
        {
            var clientType = typeof(RimMind.Application.Common.Interfaces.Client.IAIClient);

            var sendAsyncMethod = clientType.GetMethod("SendAsync", new[] { typeof(LlmRequestEnvelope) });
            sendAsyncMethod.Should().NotBeNull("IAIClient must have SendAsync(LlmRequestEnvelope)");

            var returnType = sendAsyncMethod!.ReturnType;
            returnType.Should().Be(typeof(System.Threading.Tasks.Task<Result<LlmResponse, RimMindError>>),
                "IAIClient.SendAsync must return Task<Result<LlmResponse, RimMindError>>");
        }
    }

    /// <summary>
    /// R-K6: LlmRequestEnvelope supports streaming via IsStreaming + OnStreamChunk.
    /// </summary>
    public class StreamingSupportTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K6_LlmRequestEnvelope_Has_IsStreaming()
        {
            var type = typeof(LlmRequestEnvelope);
            var prop = type.GetProperty("IsStreaming");
            prop.Should().NotBeNull("LlmRequestEnvelope must have IsStreaming property");
            prop!.PropertyType.Should().Be(typeof(bool));
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K6_LlmRequestEnvelope_Has_OnStreamChunk()
        {
            var type = typeof(LlmRequestEnvelope);
            var prop = type.GetProperty("OnStreamChunk");
            prop.Should().NotBeNull("LlmRequestEnvelope must have OnStreamChunk callback for streaming");
        }
    }

    /// <summary>
    /// R-K7: IRemoteSyncService replaces IStorageDriver for remote KV operations.
    /// </summary>
    public class RemoteSyncServiceTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K7_IRemoteSyncService_Exists()
        {
            var type = typeof(RimMind.Application.Common.Interfaces.Storage.IRemoteSyncService);
            type.Should().NotBeNull("IRemoteSyncService must exist in Application layer");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K7_IRemoteSyncService_Has_KV_Methods()
        {
            var type = typeof(RimMind.Application.Common.Interfaces.Storage.IRemoteSyncService);

            var syncOnLoadMethod = type.GetMethod("SyncOnLoadAsync");
            syncOnLoadMethod.Should().NotBeNull("IRemoteSyncService must have SyncOnLoadAsync method");

            var enqueuePushMethod = type.GetMethod("EnqueuePushAsync");
            enqueuePushMethod.Should().NotBeNull("IRemoteSyncService must have EnqueuePushAsync method");

            var manualPullMethod = type.GetMethod("ManualPullAsync");
            manualPullMethod.Should().NotBeNull("IRemoteSyncService must have ManualPullAsync method");

            var manualPushMethod = type.GetMethod("ManualPushAsync");
            manualPushMethod.Should().NotBeNull("IRemoteSyncService must have ManualPushAsync method");
        }
    }

    /// <summary>
    /// R-K8: ChatMessage and ChatToolCall live in Domain.Llm, not Application.Common.Models.Client.
    /// </summary>
    public class ChatMessageLocationTests
    {
        [Fact]
        [Trait("Phase", "K")]
        public void R_K8_ChatMessage_In_Domain_Llm()
        {
            var type = typeof(ChatMessage);
            type.Namespace.Should().Be("RimMind.Domain.Llm",
                "ChatMessage must live in Domain.Llm, not Application.Common.Models.Client");
        }

        [Fact]
        [Trait("Phase", "K")]
        public void R_K8_ChatToolCall_In_Domain_Llm()
        {
            var type = typeof(ChatToolCall);
            type.Namespace.Should().Be("RimMind.Domain.Llm",
                "ChatToolCall must live in Domain.Llm, not Application.Common.Models.Client");
        }
    }
}
