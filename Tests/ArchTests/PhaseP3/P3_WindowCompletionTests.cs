using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_WindowCompletionTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSourceFile(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private const string RequestLogRelative = "Infrastructure/UI/Window_RequestLog.cs";
        private const string ToolCallDebugRelative = "Infrastructure/UI/Window_ToolCallDebug.cs";
        private const string MechanismStatusRelative = "Infrastructure/UI/Window_MechanismStatus.cs";
        private const string ContextKeyDebugRelative = "Infrastructure/UI/Window_ContextKeyDebug.cs";
        private const string AgentModeDebugRelative = "Infrastructure/UI/Window_AgentModeDebug.cs";
        private const string AgentStateDebugRelative = "Infrastructure/UI/Window_AgentStateDebug.cs";

        [Fact]
        public void RequestLog_Has_Diagnostic_Empty_State()
        {
            var content = ReadSourceFile(RequestLogRelative);
            Assert.Contains("DrawEmptyState", content);
            Assert.Contains("RimMind.UI.RequestLog.EmptyReason.QueuePaused", content);
            Assert.Contains("RimMind.UI.RequestLog.EmptyReason.NoApiKey", content);
            Assert.Contains("RimMind.UI.RequestLog.EmptyReason.NoAgent", content);
            Assert.Contains("RimMind.UI.RequestLog.EmptyReason.NoRequests", content);
        }

        [Fact]
        public void RequestLog_Empty_State_Checks_Queue_Paused()
        {
            var content = ReadSourceFile(RequestLogRelative);
            Assert.Contains("IsPaused", content);
        }

        [Fact]
        public void RequestLog_Empty_State_Checks_ApiKey()
        {
            var content = ReadSourceFile(RequestLogRelative);
            Assert.Contains("ApiKey", content);
        }

        [Fact]
        public void RequestLog_Empty_State_Checks_Agent_Existence()
        {
            var content = ReadSourceFile(RequestLogRelative);
            Assert.Contains("CompPawnAgent", content);
        }

        [Fact]
        public void ToolCallDebug_Has_Diagnostic_Empty_State()
        {
            var content = ReadSourceFile(ToolCallDebugRelative);
            Assert.Contains("RimMind.UI.ToolCallDebug.EmptyHint", content);
        }

        [Fact]
        public void MechanismStatus_Has_Diagnostic_Empty_State()
        {
            var content = ReadSourceFile(MechanismStatusRelative);
            Assert.Contains("RimMind.UI.MechanismStatus.EmptyHint", content);
        }

        [Fact]
        public void MechanismStatus_Shows_Docs_Summary()
        {
            var content = ReadSourceFile(MechanismStatusRelative);
            Assert.Contains("mech.Docs.Summary", content);
            Assert.Contains("RimMind.UI.MechanismStatus.Description", content);
        }

        [Fact]
        public void MechanismStatus_Shows_OwnerModId()
        {
            var content = ReadSourceFile(MechanismStatusRelative);
            Assert.Contains("mech.OwnerModId", content);
            Assert.Contains("RimMind.UI.MechanismStatus.OwnerMod", content);
        }

        [Fact]
        public void ContextKeyDebug_Has_Diagnostic_Empty_State()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("RimMind.UI.ContextKeyDebug.EmptyHint", content);
        }

        [Fact]
        public void ContextKeyDebug_Has_Layer_Filter()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("_layerFilter", content);
            Assert.Contains("RimMind.UI.ContextKeyDebug.FilterLayer", content);
            Assert.Contains("CycleLayerFilter", content);
        }

        [Fact]
        public void ContextKeyDebug_Has_Owner_Filter()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("_ownerFilter", content);
            Assert.Contains("RimMind.UI.ContextKeyDebug.FilterOwner", content);
            Assert.Contains("CycleOwnerFilter", content);
        }

        [Fact]
        public void ContextKeyDebug_Applies_Filters_To_Keys()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("ApplyFilters", content);
            Assert.Contains("_layerFilter.HasValue", content);
            Assert.Contains("_ownerFilter", content);
        }

        [Fact]
        public void ContextKeyDebug_Groups_Keys_By_Layer()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("GroupBy(k => k.Layer)", content);
        }

        [Fact]
        public void ContextKeyDebug_Has_Filter_Empty_State()
        {
            var content = ReadSourceFile(ContextKeyDebugRelative);
            Assert.Contains("DrawFilterEmptyState", content);
            Assert.Contains("RimMind.UI.ContextKeyDebug.FilterEmpty", content);
        }

        [Fact]
        public void AgentModeDebug_Has_NoPawns_Hint()
        {
            var content = ReadSourceFile(AgentModeDebugRelative);
            Assert.Contains("RimMind.UI.AgentModeDebug.NoPawnsHint", content);
        }

        [Fact]
        public void AgentModeDebug_Uses_NoModes_Key_Not_NoPawns_Key()
        {
            var content = ReadSourceFile(AgentModeDebugRelative);
            var noModesCount = 0;
            var idx = 0;
            while ((idx = content.IndexOf("RimMind.UI.AgentModeDebug.NoModes", idx)) != -1)
            {
                noModesCount++;
                idx++;
            }
            Assert.True(noModesCount >= 2,
                "NoModes key must appear at least twice (for null registry and empty modes)");

            var drawRegisteredModesStart = content.IndexOf("DrawRegisteredModes");
            Assert.True(drawRegisteredModesStart > 0, "DrawRegisteredModes must exist");
            var nextBrace = content.IndexOf('{', drawRegisteredModesStart);
            var methodEnd = FindMatchingBrace(content, nextBrace);
            var methodContent = content.Substring(drawRegisteredModesStart, methodEnd - drawRegisteredModesStart);
            Assert.DoesNotContain("RimMind.UI.AgentModeDebug.NoPawns", methodContent);
        }

        [Fact]
        public void AgentStateDebug_Has_Queue_Diagnostic_In_NoPawn_State()
        {
            var content = ReadSourceFile(AgentStateDebugRelative);
            var drawNoPawnStart = content.IndexOf("private void DrawNoPawnState");
            Assert.True(drawNoPawnStart > 0, "DrawNoPawnState method must exist");
            var nextBrace = content.IndexOf('{', drawNoPawnStart);
            var methodEnd = FindMatchingBrace(content, nextBrace);
            var methodContent = content.Substring(drawNoPawnStart, methodEnd - drawNoPawnStart);
            Assert.Contains("IAIRequestQueue", methodContent);
            Assert.Contains("RimMind.UI.AgentStateDebug.QueuePaused", methodContent);
            Assert.Contains("RimMind.UI.AgentStateDebug.QueueRunning", methodContent);
        }

        private static int FindMatchingBrace(string content, int openBraceIndex)
        {
            int depth = 0;
            for (int i = openBraceIndex; i < content.Length; i++)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i + 1;
                }
            }
            return content.Length;
        }
    }
}
