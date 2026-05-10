using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using RimMind.Contracts.Client;
using RimMind.Contracts.Npc;
using RimMind.Contracts.Result;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseJ
{
    public class ApiReturnsResultTests
    {
        [Fact]
        [Trait("Phase", "J")]
        public void R_J1_IAIClient_SendAsync_ShouldReturn_Task_Result_AIResponse_RimMindError()
        {
            var aiClientType = typeof(IAIClient);
            var sendAsyncMethod = aiClientType.GetMethod("SendAsync", new[] { typeof(AIRequest) });

            sendAsyncMethod.Should().NotBeNull("IAIClient must define SendAsync(AIRequest)");

            var returnType = sendAsyncMethod!.ReturnType;
            returnType.Should().Be(typeof(Task<Result<AIResponse, RimMindError>>),
                "IAIClient.SendAsync must return Task<Result<AIResponse, RimMindError>>");
        }

        [Fact]
        [Trait("Phase", "J")]
        public void R_J1_IStorageDriver_ChatAsync_ShouldReturn_Task_Result_NpcChatResult_RimMindError()
        {
            var storageDriverType = typeof(IStorageDriver);
            var chatAsyncMethod = storageDriverType.GetMethod("ChatAsync",
                new[] { typeof(string), typeof(string), typeof(string) });

            chatAsyncMethod.Should().NotBeNull("IStorageDriver must define ChatAsync(string, string, string)");

            var returnType = chatAsyncMethod!.ReturnType;
            returnType.Should().Be(typeof(Task<Result<NpcChatResult, RimMindError>>),
                "IStorageDriver.ChatAsync must return Task<Result<NpcChatResult, RimMindError>>");
        }
    }
}
