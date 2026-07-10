using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_AgentProgressFloatTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private const string ProgressFloatRelative = "Infrastructure/UI/Window_AgentProgressFloat.cs";
        private const string OverviewPageRelative = "Infrastructure/UI/DebugCenter/Pages/OverviewDebugCenterPageDrawer.cs";
        private const string DebugActionsRelative = "Infrastructure/UI/AICoreDebugActions.cs";
        private const string FlowLabRelative = "Infrastructure/UI/Window_AgentFlowLab.cs";

        [Fact]
        public void ProgressFloat_Window_Class_Exists()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("class Window_AgentProgressFloat", content);
            Assert.Contains("Window", content);
        }

        [Fact]
        public void ProgressFloat_Has_AgentProgressEntry_Struct()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("AgentProgressEntry", content);
            Assert.Contains("Pawn", content);
            Assert.Contains("PawnLabel", content);
            Assert.Contains("Phase", content);
            Assert.Contains("ElapsedTicks", content);
        }

        [Fact]
        public void ProgressFloat_Displays_WorkflowPhase()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("AgentWorkflowPhase", content);
            Assert.Contains("IPawnAgent", content);
            Assert.Contains("WorkflowPhase", content);
        }

        [Fact]
        public void ProgressFloat_Has_Phase_Color_Indicator()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("PhaseColor", content);
            Assert.Contains("AgentWorkflowPhase.Idle", content);
            Assert.Contains("AgentWorkflowPhase.Thinking", content);
            Assert.Contains("AgentWorkflowPhase.Acting", content);
            Assert.Contains("AgentWorkflowPhase.Perceiving", content);
            Assert.Contains("AgentWorkflowPhase.Recording", content);
        }

        [Fact]
        public void ProgressFloat_Has_Phase_Labels()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("PhaseLabel", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.PhaseIdle", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.PhaseThinking", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.PhaseActing", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.PhasePerceiving", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.PhaseRecording", content);
        }

        [Fact]
        public void ProgressFloat_Shows_Elapsed_Time()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("ElapsedTicks", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.Elapsed", content);
        }

        [Fact]
        public void ProgressFloat_Has_Details_Button_Navigating_To_AgentStateDebug()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("RimMind.UI.AgentProgressFloat.Details", content);
            Assert.Contains("Window_AgentStateDebug", content);
        }

        [Fact]
        public void ProgressFloat_Subscribes_To_WorkflowPhaseChange_Event()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("SubscribeBus", content);
            Assert.Contains("UnsubscribeBus", content);
            Assert.Contains("WorkflowPhaseChange", content);
            Assert.Contains("OnWorkflowPhaseChange", content);
        }

        [Fact]
        public void ProgressFloat_Refreshes_Entries_Periodically()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("RefreshEntries", content);
            Assert.Contains("WindowUpdate", content);
            Assert.Contains("_lastRefreshTick", content);
        }

        [Fact]
        public void ProgressFloat_Shows_Queue_State()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("IAIRequestQueue", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.QueuePaused", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.QueueRunning", content);
        }

        [Fact]
        public void ProgressFloat_Has_Empty_State()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("DrawEmptyState", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.NoAgents", content);
            Assert.Contains("RimMind.UI.AgentProgressFloat.NoAgentsHint", content);
        }

        [Fact]
        public void ProgressFloat_Uses_CompPawnAgent_To_Find_Agents()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("CompPawnAgent.GetComp", content);
            Assert.Contains("mapPawns.AllPawns", content);
        }

        [Fact]
        public void ProgressFloat_Cleans_Up_On_Close()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("PreClose", content);
            Assert.Contains("UnsubscribeBus", content);
        }

        [Fact]
        public void ProgressFloat_Is_Not_A_DebugCenter_Overview_Shortcut()
        {
            var content = ReadSourceFile(OverviewPageRelative);

            Assert.DoesNotContain("RimMind.UI.Hub.AgentProgress", content);
            Assert.DoesNotContain("Window_AgentProgressFloat", content);
            Assert.Contains("context.Navigation.GoTo", content);
        }

        [Fact]
        public void ProgressFloat_Entry_In_Debug_Actions()
        {
            var content = ReadSourceFile(DebugActionsRelative);
            Assert.Contains("Agent Progress Float", content);
            Assert.Contains("Window_AgentProgressFloat", content);
        }

        [Fact]
        public void ProgressFloat_Navigation_From_FlowLab()
        {
            var content = ReadSourceFile(FlowLabRelative);
            Assert.Contains("Window_AgentProgressFloat", content);
            Assert.Contains("RimMind.UI.AgentFlowLab.OpenAgentProgress", content);
        }

        [Fact]
        public void ProgressFloat_Uses_Localization_Keys()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("RimMind.UI.AgentProgressFloat.Title", content);
            Assert.Contains(".Translate()", content);
        }

        [Fact]
        public void ProgressFloat_Window_Settings_Are_Non_Modal()
        {
            var content = ReadSourceFile(ProgressFloatRelative);
            Assert.Contains("forcePause = false", content);
            Assert.Contains("closeOnClickedOutside = true", content);
            Assert.Contains("absorbInputAroundWindow = false", content);
        }
    }
}
