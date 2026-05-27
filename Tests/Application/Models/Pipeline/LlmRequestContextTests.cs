using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.Agent.Modes;
using Xunit;

namespace RimMind.Tests.Application.Models.Pipeline
{
    public class LlmRequestContextTests
    {
        [Fact]
        public void AgentModeId_DefaultsToDefaultAgentModeId()
        {
            var ctx = new LlmRequestContext();
            Assert.Equal(default(AgentModeId), ctx.AgentModeId);
        }

        [Fact]
        public void AgentModeId_CanBeSetAndReadBack()
        {
            var ctx = new LlmRequestContext();
            var modeId = AgentModeId.Reactive;
            ctx.AgentModeId = modeId;
            Assert.Equal(AgentModeId.Reactive, ctx.AgentModeId);
        }

        [Fact]
        public void AgentModeId_CanBeSetToCustomModeId()
        {
            var ctx = new LlmRequestContext();
            var customId = new AgentModeId("rimmind.custom");
            ctx.AgentModeId = customId;
            Assert.Equal("rimmind.custom", ctx.AgentModeId.Value);
        }
    }
}
