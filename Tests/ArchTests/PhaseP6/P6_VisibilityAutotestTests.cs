using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_VisibilityAutotestTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void DebugActions_Register_Runtime_Visibility_Autotest()
        {
            var path = Path.Combine(RepoRoot, "Source", "Infrastructure", "UI", "AICoreDebugActions.cs");
            var content = File.ReadAllText(path);

            Assert.Contains("[DebugAction(\"Autotests\", \"Test P Visibility Entrypoints\"", content);
            Assert.Contains("Window_RequestLog", content);
            Assert.Contains("Window_ToolCallDebug", content);
            Assert.Contains("Window_MechanismStatus", content);
            Assert.Contains("Window_ContextKeyDebug", content);
            Assert.Contains("Window_AgentStateDebug", content);
            Assert.Contains("Window_AgentModeDebug", content);
            Assert.Contains("Window_AgentFlowLab", content);
            Assert.Contains("Window_AgentProgressFloat", content);
            Assert.Contains("ContentFinder<Texture2D>.Get(\"UI/RimMind/Icon\", false)", content);
        }
    }
}
