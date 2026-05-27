using System;
using System.IO;
using RimMind.Application.Common.Interfaces.Agent.Modes;
using RimMind.Application.Features.Agent.Modes;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    /// <summary>
    /// Verifies that ProactiveBehaviorExecutor uses IProactiveExtensions instead of
    /// the concrete ProactiveAgentMode type, fixing the OCP violation.
    ///
    /// Uses a mix of runtime tests (for the interface contract) and source-file
    /// reading (for the Presentation-layer code that depends on Verse types).
    /// </summary>
    public class ProactiveBehaviorExecutorTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string ExecutorPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "ProactiveBehaviorExecutor.cs");

        private static string ReadSource()
        {
            Assert.True(File.Exists(ExecutorPath), $"ProactiveBehaviorExecutor.cs must exist at {ExecutorPath}");
            return File.ReadAllText(ExecutorPath);
        }

        // --- Runtime tests: IProactiveExtensions interface contract ---

        [Fact]
        public void ProactiveAgentMode_Implements_IProactiveExtensions()
        {
            var mode = new ProactiveAgentMode(new TestTickProvider());
            Assert.IsAssignableFrom<IProactiveExtensions>(mode);
        }

        [Fact]
        public void IProactiveExtensions_Cast_From_ProactiveAgentMode_ReturnsSameInstance()
        {
            var mode = new ProactiveAgentMode(new TestTickProvider());
            var proactive = mode as IProactiveExtensions;
            Assert.NotNull(proactive);
            Assert.Same(mode, proactive);
        }

        [Fact]
        public void Mode_NotImplementing_IProactiveExtensions_Fails_IsCheck()
        {
            IAgentMode mode = new NonProactiveTestMode();
            Assert.False(mode is IProactiveExtensions);
        }

        [Fact]
        public void IProactiveExtensions_Properties_ReturnNull_WhenNotProvided()
        {
            var mode = new ProactiveAgentMode(new TestTickProvider());
            var proactive = (IProactiveExtensions)mode;
            Assert.Null(proactive.ReflectionStrategy);
            Assert.Null(proactive.DailyPlanner);
            Assert.Null(proactive.PsychologyWatcher);
            Assert.Null(proactive.SocialEventOrganizer);
            Assert.Null(proactive.TraitEvolutionEngine);
        }

        // --- Source-level tests: verify ProactiveBehaviorExecutor uses IProactiveExtensions ---

        [Fact]
        public void ExecuteProactiveExtensions_Uses_IProactiveExtensions_Not_ProactiveAgentMode()
        {
            var source = ReadSource();

            Assert.Contains("IProactiveExtensions", source);
            Assert.DoesNotContain("ProactiveAgentMode", source);
        }

        [Fact]
        public void ExecuteProactiveExtensions_Checks_IProactiveExtensions_Interface()
        {
            var source = ReadSource();

            Assert.Contains("mode is not IProactiveExtensions", source);
        }

        [Fact]
        public void PrivateMethods_Use_IProactiveExtensions_ParameterType()
        {
            var source = ReadSource();

            // All private methods that receive the proactive parameter should use IProactiveExtensions
            Assert.Contains("IProactiveExtensions proactive, IPawnAgent agent", source);
        }

        [Fact]
        public void No_Remaining_ProactiveAgentMode_References()
        {
            var source = ReadSource();

            // The source should not reference the concrete ProactiveAgentMode type
            Assert.DoesNotContain("ProactiveAgentMode", source);
        }

        // --- Test helpers ---

        private class TestTickProvider : RimMind.Application.Common.Interfaces.Abstractions.ITickProvider
        {
            public int TicksGame => 100000;
        }

        private class NonProactiveTestMode : IAgentMode
        {
            public RimMind.Domain.Agent.Modes.AgentModeId ModeId => RimMind.Domain.Agent.Modes.AgentModeId.Reactive;
            public string DisplayName => "NonProactive";
            public string Description => "Test mode without proactive extensions";
            public string Id => ModeId.Value;
            public string OwnerModId => "Test";

            public bool IsApplicable(RimMind.Application.Common.Interfaces.Agent.IAgentInfo agent) => true;

            public bool ShouldThink(RimMind.Application.Common.Interfaces.Agent.IAgentInfo agent,
                System.Collections.Generic.IReadOnlyList<RimMind.Application.Common.Models.Pipeline.PerceptionBufferEntry> perceptions) => false;

            public IThinkStrategy GetThinkStrategy() => throw new NotImplementedException();

            public System.Collections.Generic.IReadOnlyList<string> AllowedToolIds(
                RimMind.Application.Common.Interfaces.Tools.IToolRegistry registry) => Array.Empty<string>();
        }
    }
}
