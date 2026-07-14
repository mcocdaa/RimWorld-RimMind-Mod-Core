using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_VisibilityAutotestTests
    {
        private static string ProjectRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void DebugActions_Register_Runtime_Visibility_Autotest()
        {
            var path = Path.Combine(ProjectRoot, "Source", "Infrastructure", "UI", "AICoreDebugActions.cs");
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
            Assert.Contains("ReportAutotest(\"P.VisibilityEntrypoints\", pass, fail)", content);
            Assert.Contains("[RIMTEST][Core][{caseId}][{outcome}]", content);
        }

        [Fact]
        public void LegacyDebugLogWindow_UsesUnifiedRequestTraceView()
        {
            var path = Path.Combine(ProjectRoot, "Source", "Infrastructure", "UI", "Window_AIDebugLog.cs");
            var content = File.ReadAllText(path);

            Assert.Contains("AIRequestsPageDrawer", content);
            Assert.Contains("_traceDrawer.Draw(inRect, scope)", content);
            Assert.DoesNotContain("TryGet<IAIDebugLog>", content);
        }

        [Fact]
        public void ContextPreviews_UseAsyncBuilderPath()
        {
            var sourceFiles = new[]
            {
                "Infrastructure/UI/AICoreDebugActions.cs",
                "Infrastructure/UI/Window_AgentStateDebug.cs",
                "Infrastructure/UI/Window_AgentFlowLab.cs"
            };

            foreach (string relativePath in sourceFiles)
            {
                string content = File.ReadAllText(Path.Combine(ProjectRoot, "Source", relativePath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.Contains("BuildSnapshotFromEnvelopeAsync", content);
                Assert.DoesNotContain(".BuildSnapshotFromEnvelope(", content);
            }
        }
    }
}
