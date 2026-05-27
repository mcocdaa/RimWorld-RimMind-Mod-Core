using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N6: Clean Architecture compliance — verifies that layer dependency violations
    /// introduced in Phase 2 (Task 4-6) have been properly resolved:
    /// - ProactiveBehaviorExecutor no longer references Infrastructure.Social directly
    /// - PawnAgentFactory no longer references Infrastructure.Agent directly
    /// - FloatMenu_InnerVoice no longer references Presentation layer
    /// - RemoteSyncSettingsUI moved from Infrastructure to Presentation
    /// - New Application-layer interfaces exist and are implemented by Infrastructure classes
    /// </summary>
    public class R_N6_CleanArchLayerCompliance
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        // === Interface existence tests ===

        [Fact]
        public void IDreamThoughtInjector_Interface_Exists()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Application",
                "Common", "Interfaces", "Agent", "Social", "IDreamThoughtInjector.cs");
            Assert.True(File.Exists(path), "IDreamThoughtInjector.cs must exist in Application layer");
        }

        [Fact]
        public void ITraitEvolver_Interface_Exists()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Application",
                "Common", "Interfaces", "Agent", "Social", "ITraitEvolver.cs");
            Assert.True(File.Exists(path), "ITraitEvolver.cs must exist in Application layer");
        }

        [Fact]
        public void IAgentIdentityProvider_Interface_Exists()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Application",
                "Common", "Interfaces", "Agent", "IAgentIdentityProvider.cs");
            Assert.True(File.Exists(path), "IAgentIdentityProvider.cs must exist in Application layer");
        }

        // === Infrastructure implements Application interfaces (file-based checks) ===

        [Fact]
        public void VerseDreamThoughtInjector_Implements_IDreamThoughtInjector()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Infrastructure",
                "Social", "VerseDreamThoughtInjector.cs");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.Contains("IDreamThoughtInjector", content);
            Assert.Contains(": IDreamThoughtInjector", content);
        }

        [Fact]
        public void VerseTraitEvolver_Implements_ITraitEvolver()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Infrastructure",
                "Social", "VerseTraitEvolver.cs");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.Contains("ITraitEvolver", content);
            Assert.Contains(": ITraitEvolver", content);
        }

        // === Layer dependency direction tests ===

        [Fact]
        public void ProactiveBehaviorExecutor_No_Infrastructure_Social_Using()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Presentation",
                "Agent", "ProactiveBehaviorExecutor.cs");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.DoesNotContain("using RimMind.Infrastructure.Social;", content);
            Assert.DoesNotContain("VerseDreamThoughtInjector", content);
            Assert.DoesNotContain("VerseTraitEvolver", content);
            Assert.Contains("IDreamThoughtInjector", content);
            Assert.Contains("ITraitEvolver", content);
        }

        [Fact]
        public void PawnAgentFactory_No_Infrastructure_Agent_Using()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Presentation",
                "Agent", "PawnAgentFactory.cs");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.DoesNotContain("using RimMind.Infrastructure.Agent;", content);
            Assert.DoesNotContain("MechanismActionExecutor", content);
            Assert.Contains("IActionExecutor", content);
        }

        [Fact]
        public void FloatMenu_InnerVoice_No_Presentation_Using()
        {
            var path = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Infrastructure",
                "Patches", "FloatMenu_InnerVoice.cs");
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.DoesNotContain("using RimMind.Presentation;", content);
            Assert.Contains("IAgentIdentityProvider", content);
        }

        [Fact]
        public void RemoteSyncSettingsUI_Not_In_Infrastructure_Layer()
        {
            var infraPath = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Infrastructure",
                "UI", "RemoteSyncSettingsUI.cs");
            Assert.False(File.Exists(infraPath),
                "RemoteSyncSettingsUI should not exist in Infrastructure layer");

            var presPath = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Presentation",
                "UI", "RemoteSyncSettingsUI.cs");
            Assert.True(File.Exists(presPath),
                "RemoteSyncSettingsUI should exist in Presentation layer");
        }

        [Fact]
        public void RemoteSyncSettingsUI_No_Infrastructure_UI_Namespace()
        {
            var presPath = Path.Combine(RepoRoot, "RimMind-Core", "Source", "Presentation",
                "UI", "RemoteSyncSettingsUI.cs");
            var content = File.ReadAllText(presPath);
            Assert.DoesNotContain("namespace RimMind.Infrastructure.UI", content);
            Assert.Contains("namespace RimMind.Presentation.UI", content);
        }
    }
}
