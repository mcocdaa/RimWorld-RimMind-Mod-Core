using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    public class R_N7_UiBaseClassAndLayoutScopeTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static readonly string[] WindowFiles =
        {
            "Infrastructure/UI/Window_RequestLog.cs",
            "Infrastructure/UI/Window_AIDebugLog.cs",
            "Infrastructure/UI/Window_ToolCallDebug.cs",
            "Infrastructure/UI/Window_MechanismStatus.cs",
            "Infrastructure/UI/Window_ContextKeyDebug.cs",
            "Infrastructure/UI/Window_AgentStateDebug.cs",
            "Infrastructure/UI/Window_AgentModeDebug.cs",
            "Infrastructure/UI/Window_AgentFlowLab.cs",
            "Infrastructure/UI/Window_AgentProgressFloat.cs",
            "Infrastructure/UI/Window_AgentDialogue.cs",
            "Infrastructure/UI/Window_RimMindSettings.cs",
            "Infrastructure/UI/MainTabWindow_RimMindHub.cs",
        };

        private static readonly string[] ITabFiles =
        {
            "Infrastructure/Verse/ITab_Pawn_Agent.cs",
        };

        private static readonly string[] MainTabWindowFiles =
        {
            "Infrastructure/UI/MainTabWindow_RimMindHub.cs",
        };

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_All_Window_Files_Inherit_RimMindWindowBase()
        {
            foreach (var rel in WindowFiles)
            {
                var path = Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"expected window file {rel} to exist");
                var text = File.ReadAllText(path);
                Assert.Contains(": RimMindWindowBase", text);
            }
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_All_ITab_Files_Inherit_RimMindITabBase()
        {
            foreach (var rel in ITabFiles)
            {
                var path = Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"expected itab file {rel} to exist");
                var text = File.ReadAllText(path);
                Assert.Contains(": RimMindITabBase", text);
            }
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_MainTabWindow_RimMindHub_Inherits_Window_RimMindHub()
        {
            foreach (var rel in MainTabWindowFiles)
            {
                var path = Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"expected maintabwindow file {rel} to exist");
                var text = File.ReadAllText(path);
                Assert.Contains(": Window_RimMindHub", text);
            }
        }

        [Fact]
        public void R_N7_DebugCenterPageDrawer_Contract_Requires_LayoutScope()
        {
            var path = Path.Combine(SourceDir, "Infrastructure", "UI", "DebugCenter", "IDebugCenterPageDrawer.cs");
            string text = File.ReadAllText(path);

            Assert.Contains("using RimMind.Infrastructure.UI.Layout;", text);
            Assert.Contains("void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope);", text);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_No_Window_File_Overrides_DoWindowContents_Directly()
        {
            foreach (var rel in WindowFiles)
            {
                var path = Path.Combine(SourceDir, rel.Replace('/', Path.DirectorySeparatorChar));
                var text = File.ReadAllText(path);
                Assert.DoesNotContain("override void DoWindowContents", text);
            }
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_LayoutTraceRecorder_Exists_With_Expected_Methods()
        {
            var path = Path.Combine(SourceDir, "Infrastructure", "UI", "Layout", "LayoutTraceRecorder.cs");
            Assert.True(File.Exists(path), "LayoutTraceRecorder must exist");
            var text = File.ReadAllText(path);
            Assert.Contains("public void Record(", text);
            Assert.Contains("public List<LayoutConflict> DetectConflicts(", text);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_LayoutConflictStore_Exists_With_Expected_Methods()
        {
            var path = Path.Combine(SourceDir, "Infrastructure", "UI", "Layout", "LayoutConflictStore.cs");
            Assert.True(File.Exists(path), "LayoutConflictStore must exist");
            var text = File.ReadAllText(path);
            Assert.Contains("public static void Publish(", text);
            Assert.Contains("public static bool TryGet(", text);
            Assert.Contains("public static IEnumerable<LayoutReport> GetAll(", text);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_RimMindLayoutScope_Exists_With_Begin_And_Dispose()
        {
            var path = Path.Combine(SourceDir, "Infrastructure", "UI", "Layout", "RimMindLayoutScope.cs");
            Assert.True(File.Exists(path), "RimMindLayoutScope must exist");
            var text = File.ReadAllText(path);
            Assert.Contains("public static RimMindLayoutScope Begin(", text);
            Assert.Contains("public void Dispose(", text);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void R_N7_AICoreDebugActions_Has_Dump_And_Overlay_Actions()
        {
            var path = Path.Combine(SourceDir, "Infrastructure", "UI", "AICoreDebugActions.cs");
            Assert.True(File.Exists(path), "AICoreDebugActions must exist");
            var text = File.ReadAllText(path);
            Assert.Contains("Dump UI Layout Conflicts", text);
            Assert.Contains("Toggle UI Layout Conflict Overlay", text);
        }
    }
}
