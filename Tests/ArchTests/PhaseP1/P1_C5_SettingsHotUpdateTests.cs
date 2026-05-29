using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-C5: Settings Hot-Update.
    /// Verifies that PawnThinker and PawnAgent do not cache tick settings values
    /// at construction time, but instead read them from IAgentTickSettings on each access.
    /// </summary>
    public class P1_C5_SettingsHotUpdateTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Agent");

        private static readonly string PawnThinkerPath = Path.Combine(AgentDir, "PawnThinker.cs");
        private static readonly string PawnAgentPath = Path.Combine(AgentDir, "PawnAgent.cs");

        [Fact]
        public void PawnThinker_Does_Not_Cache_ThinkCooldownTicks_As_Field()
        {
            var source = File.ReadAllText(PawnThinkerPath);
            Assert.DoesNotContain("private int _thinkCooldownTicks;", source);
        }

        [Fact]
        public void PawnThinker_Reads_ThinkCooldownTicks_From_Settings_Property()
        {
            var source = File.ReadAllText(PawnThinkerPath);
            Assert.Contains("ThinkCooldownTicks => _tickSettings?.ThinkCooldownTicks", source);
        }

        [Fact]
        public void PawnThinker_Does_Not_Assign_ThinkCooldownTicks_In_Constructor()
        {
            var source = File.ReadAllText(PawnThinkerPath);
            Assert.DoesNotContain("_thinkCooldownTicks =", source);
        }

        [Fact]
        public void PawnAgent_Does_Not_Cache_TickInterval_As_Field()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.DoesNotContain("private int _tickInterval;", source);
        }

        [Fact]
        public void PawnAgent_Reads_TickInterval_From_Settings_Property()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.Contains("TickInterval => _tickSettings?.AgentTickInterval", source);
        }

        [Fact]
        public void PawnAgent_Does_Not_Assign_TickInterval_In_Constructor()
        {
            var source = File.ReadAllText(PawnAgentPath);
            Assert.DoesNotContain("_tickInterval =", source);
        }
    }
}
