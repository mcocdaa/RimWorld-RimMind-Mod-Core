using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-E2: IModeTransitionPolicy Interface.
    /// Verifies that IModeTransitionPolicy exists, DefaultModeTransitionPolicy is implemented,
    /// PawnAgent.SwitchMode checks policies, and RimMindAPI exposes ModePolicies.
    /// </summary>
    public class P1_E2_ModeTransitionPolicyTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string ModesDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Common", "Interfaces", "Agent", "Modes");

        private static readonly string FeaturesModesDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent", "Modes");

        private static readonly string PawnAgentPath = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent", "PawnAgent.cs");

        private static readonly string RimMindAPIPath = Path.Combine(
            ProjectRoot, "Source", "RimMindAPI.cs");

        private static readonly string CompositionRootPath = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Runtime", "Composition", "AgentComposition.cs");

        [Fact]
        public void IModeTransitionPolicy_Interface_Exists()
        {
            var path = Path.Combine(ModesDir, "IModeTransitionPolicy.cs");
            Assert.True(File.Exists(path), "IModeTransitionPolicy.cs should exist");
        }

        [Fact]
        public void IModeTransitionPolicy_Has_CanTransition_Method()
        {
            var path = Path.Combine(ModesDir, "IModeTransitionPolicy.cs");
            var source = File.ReadAllText(path);
            Assert.Contains("CanTransition", source);
            Assert.Contains("IAgentInfo", source);
            Assert.Contains("AgentModeId", source);
        }

        [Fact]
        public void IModeTransitionPolicy_Has_DenyReason_Property()
        {
            var path = Path.Combine(ModesDir, "IModeTransitionPolicy.cs");
            var source = File.ReadAllText(path);
            Assert.Contains("DenyReason", source);
        }

        [Fact]
        public void IModeTransitionPolicy_Inherits_IExtension()
        {
            var path = Path.Combine(ModesDir, "IModeTransitionPolicy.cs");
            var source = File.ReadAllText(path);
            Assert.Contains("IExtension", source);
        }

        [Fact]
        public void DefaultModeTransitionPolicy_Exists()
        {
            var path = Path.Combine(FeaturesModesDir, "DefaultModeTransitionPolicy.cs");
            Assert.True(File.Exists(path), "DefaultModeTransitionPolicy.cs should exist");
        }

        [Fact]
        public void DefaultModeTransitionPolicy_Allows_All_Transitions()
        {
            var path = Path.Combine(FeaturesModesDir, "DefaultModeTransitionPolicy.cs");
            var source = File.ReadAllText(path);
            Assert.Contains("return true;", source);
        }

        [Fact]
        public void PawnAgent_SwitchMode_Checks_Policies()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("ModePolicies", source);
            Assert.Contains("CanTransition", source);
        }

        [Fact]
        public void PawnAgent_SwitchMode_Logs_Denial()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("ModeTransitionDenied", source);
        }

        [Fact]
        public void RimMindAPI_Exposes_ModePolicies()
        {
            var source = File.ReadAllText(RimMindAPIPath);
            Assert.Contains("ModePolicies", source);
        }

        [Fact]
        public void CompositionRoot_Registers_DefaultModeTransitionPolicy()
        {
            var source = File.ReadAllText(CompositionRootPath);
            Assert.Contains("DefaultModeTransitionPolicy", source);
        }
    }
}
