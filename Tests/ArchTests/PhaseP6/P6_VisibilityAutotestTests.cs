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
            Assert.Contains("DebugCenterPageRegistry.Find(pageId)", content);
            Assert.Contains("DebugCenterPageRegistry.Create(pageId)", content);
            Assert.Contains("\"ai_requests\"", content);
            Assert.Contains("\"context_keys\"", content);
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

        [Fact]
        public void AgentStatePreview_DelegatesTaskStateToCoordinator()
        {
            string window = File.ReadAllText(Path.Combine(ProjectRoot, "Source", "Infrastructure", "UI", "Window_AgentStateDebug.cs"));
            string coordinator = File.ReadAllText(Path.Combine(ProjectRoot, "Source", "Infrastructure", "UI", "AgentState", "AgentContextPreviewCoordinator.cs"));

            Assert.Contains("AgentContextPreviewCoordinator", window);
            Assert.DoesNotContain("Task<ContextSnapshot?>?", window);
            Assert.Contains("_contextPreview.Poll", window);
            Assert.Contains("!_pendingTask.IsCompleted", coordinator);
        }
    }
}
