using System;
using System.Collections.Generic;
using System.Threading;
using RimMind.Core.Client;
using RimMind.Core.Internal;
using Xunit;

namespace RimMind.Core.Tests
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

    public class SkipCheckRegistryTests
    {
        [Fact]
        public void ShouldSkipDialogue_NoChecks_ReturnsFalse()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            Assert.False(registry.ShouldSkipDialogue(null!, "any"));
        }

        [Fact]
        public void RegisterDialogueSkipCheck_CheckReturnsTrue_Skips()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            registry.RegisterDialogueSkipCheck("src1", (pawn, type) => true);
            Assert.True(registry.ShouldSkipDialogue(null!, "any"));
            registry.UnregisterDialogueSkipCheck("src1");
        }

        [Fact]
        public void RegisterDialogueSkipCheck_CheckReturnsFalse_DoesNotSkip()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            registry.RegisterDialogueSkipCheck("src2", (pawn, type) => false);
            Assert.False(registry.ShouldSkipDialogue(null!, "any"));
            registry.UnregisterDialogueSkipCheck("src2");
        }

        [Fact]
        public void ShouldSkipFloatMenu_NoChecks_ReturnsFalse()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            Assert.False(registry.ShouldSkipFloatMenu());
        }

        [Fact]
        public void ShouldSkipAction_NoChecks_ReturnsFalse()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            Assert.False(registry.ShouldSkipAction("intent1"));
        }

        [Fact]
        public void RegisterActionSkipCheck_CheckReturnsTrue_Skips()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            registry.RegisterActionSkipCheck("src3", intent => intent == "dangerous");
            Assert.True(registry.ShouldSkipAction("dangerous"));
            Assert.False(registry.ShouldSkipAction("safe"));
            registry.UnregisterActionSkipCheck("src3");
        }

        [Fact]
        public void Reset_ClearsAllChecks()
        {
            var registry = new RimMind.Core.Internal.SkipCheckRegistry();
            registry.RegisterDialogueSkipCheck("r1", (p, t) => true);
            registry.RegisterFloatMenuSkipCheck("r2", () => true);
            registry.RegisterActionSkipCheck("r3", i => true);

            registry.Reset();

            Assert.False(registry.ShouldSkipDialogue(null!, "any"));
            Assert.False(registry.ShouldSkipFloatMenu());
            Assert.False(registry.ShouldSkipAction("any"));
        }
    }

    public class IncidentRegistryTests
    {
        [Fact]
        public void RegisterCallback_AndNotify_InvokesCallback()
        {
            var registry = new RimMind.Core.Internal.IncidentRegistry();
            int count = 0;

            string key = registry.RegisterIncidentExecutedCallback(() => count++);
            registry.NotifyIncidentExecuted();

            Assert.Equal(1, count);
            registry.UnregisterIncidentExecutedCallback(key);
        }

        [Fact]
        public void UnregisterCallback_NotInvoked()
        {
            var registry = new RimMind.Core.Internal.IncidentRegistry();
            int count = 0;

            string key = registry.RegisterIncidentExecutedCallback(() => count++);
            registry.UnregisterIncidentExecutedCallback(key);
            registry.NotifyIncidentExecuted();

            Assert.Equal(0, count);
        }

        [Fact]
        public void Reset_ClearsCallbacks()
        {
            var registry = new RimMind.Core.Internal.IncidentRegistry();
            int count = 0;

            registry.RegisterIncidentExecutedCallback(() => count++);
            registry.Reset();
            registry.NotifyIncidentExecuted();

            Assert.Equal(0, count);
        }
    }
}
