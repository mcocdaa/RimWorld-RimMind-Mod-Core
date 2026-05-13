using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Presentation.Runtime;
using RimMind.Application.Features.Registry;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Features.Flywheel;
using RimMind.Presentation.Runtime;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Settings;
using Verse;
using Xunit;

using INpcManager = RimMind.Domain.Events.Npc.INpcManager;

namespace RimMind.Presentation.Tests
{
    public class RimMindServiceLocatorTests
    {
        [Fact]
        public void Register_AndGet_ReturnsInstance()
        {
            RimMindServiceLocator.Reset();
            var obj = new object();
            RimMindServiceLocator.Register(obj);

            var result = RimMindServiceLocator.Get<object>();
            Assert.Same(obj, result);
            RimMindServiceLocator.Reset();
        }

        [Fact]
        public void Get_UnregisteredType_ReturnsNull()
        {
            RimMindServiceLocator.Reset();
            var result = RimMindServiceLocator.Get<string>();
            Assert.Null(result);
            RimMindServiceLocator.Reset();
        }

        [Fact]
        public void IsRegistered_ReturnsCorrectState()
        {
            RimMindServiceLocator.Reset();
            Assert.False(RimMindServiceLocator.IsRegistered<string>());

            RimMindServiceLocator.Register("test");
            Assert.True(RimMindServiceLocator.IsRegistered<string>());
            RimMindServiceLocator.Reset();
        }

        [Fact]
        public void Reset_ClearsAllRegistrations()
        {
            RimMindServiceLocator.Reset();
            RimMindServiceLocator.Register("test");
            var obj = new object();
            RimMindServiceLocator.Register(obj);

            RimMindServiceLocator.Reset();

            Assert.Null(RimMindServiceLocator.Get<string>());
            Assert.Null(RimMindServiceLocator.Get<object>());
            RimMindServiceLocator.Reset();
        }

        [Fact]
        public void Register_OverwritesExisting()
        {
            RimMindServiceLocator.Reset();
            RimMindServiceLocator.Register("first");
            RimMindServiceLocator.Register("second");

            Assert.Equal("second", RimMindServiceLocator.Get<string>());
            RimMindServiceLocator.Reset();
        }
    }

    public class AIRequestPoolTests
    {
        [Fact]
        public void Rent_ReturnsNewInstance()
        {
            var req = AIRequestPool.Rent();
            Assert.NotNull(req);
        }

        [Fact]
        public void Return_AndRent_RecyclesInstance()
        {
            var req = AIRequestPool.Rent();
            req.RequestId = "test_pool";
            req.MaxTokens = 999;

            AIRequestPool.Return(req);

            var reused = AIRequestPool.Rent();
            Assert.Equal(string.Empty, reused.RequestId);
            Assert.Equal(800, reused.MaxTokens);
        }

        [Fact]
        public void Reset_ClearsAllFields()
        {
            var req = new AIRequest
            {
                SystemPrompt = "sys",
                UserPrompt = "user",
                Messages = new List<ChatMessage>(),
                MaxTokens = 100,
                Temperature = 0.5f,
                RequestId = "id",
                ModId = "mod",
                ExpireAtTicks = 100,
                UseJsonMode = false,
                JsonSchema = "schema",
                Tools = new List<StructuredTool>(),
                Priority = AIRequestPriority.High,
                MaxRetryCount = 3,
            };

            req.Reset();

            Assert.Equal(string.Empty, req.SystemPrompt);
            Assert.Equal(string.Empty, req.UserPrompt);
            Assert.Null(req.Messages);
            Assert.Equal(800, req.MaxTokens);
            Assert.Equal(0.7f, req.Temperature);
            Assert.Equal(string.Empty, req.RequestId);
            Assert.Equal(string.Empty, req.ModId);
            Assert.Equal(0, req.ExpireAtTicks);
            Assert.True(req.UseJsonMode);
            Assert.Null(req.JsonSchema);
            Assert.Null(req.Tools);
            Assert.Equal(AIRequestPriority.Normal, req.Priority);
            Assert.Null(req.MaxRetryCount);
        }
    }
}
