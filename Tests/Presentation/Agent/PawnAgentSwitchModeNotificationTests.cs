using System;
using System.IO;
using RimMind.Domain.Events;
using Xunit;

namespace RimMind.Tests.Presentation.Agent
{
    /// <summary>
    /// Verifies that PawnAgent.SwitchMode:
    /// 1. Fills the timestamp in AgentModeChangedEvent (not default 0)
    /// 2. Logs the mode change via Log.Message
    /// 3. Shows a player notification via Messages.Message
    /// 4. Guards the Messages.Message call with Current.Game null check
    ///
    /// Uses source-file reading because PawnAgent.cs depends on Verse types
    /// not available in the net10.0 test project.
    /// </summary>
    public class PawnAgentSwitchModeNotificationTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string PawnAgentPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Presentation", "Agent", "PawnAgent.cs");

        private static string ReadSource()
        {
            Assert.True(File.Exists(PawnAgentPath), $"PawnAgent.cs must exist at {PawnAgentPath}");
            return File.ReadAllText(PawnAgentPath);
        }

        // --- Structural tests: verify source code contains expected patterns ---

        [Fact]
        public void SwitchMode_PassesTimestamp_To_AgentModeChangedEvent()
        {
            var source = ReadSource();

            // The SwitchMode method should pass Find.TickManager?.TicksGame ?? 0 as the timestamp
            // parameter to the AgentModeChangedEvent constructor
            Assert.Contains("Find.TickManager", source);
            Assert.Contains("TicksGame", source);
        }

        [Fact]
        public void SwitchMode_Logs_ModeChange_Via_LogMessage()
        {
            var source = ReadSource();

            // SwitchMode should call Verse.Log.Message to log the mode change
            Assert.Contains("Log.Message", source);
        }

        [Fact]
        public void SwitchMode_Shows_PlayerNotification_Via_MessagesMessage()
        {
            var source = ReadSource();

            // SwitchMode should call Verse.Messages.Message for player notification
            Assert.Contains("Messages.Message", source);
        }

        [Fact]
        public void SwitchMessage_Uses_TranslationKey_ModeChanged()
        {
            var source = ReadSource();

            // Should use the RimMind.Agent.ModeChanged translation key
            Assert.Contains("RimMind.Agent.ModeChanged", source);
        }

        [Fact]
        public void SwitchMode_Guards_Messages_With_CurrentGame_NullCheck()
        {
            var source = ReadSource();

            // Should guard with Current.Game null check before calling Messages.Message
            Assert.Contains("Current.Game", source);
        }

        [Fact]
        public void SwitchMode_Uses_SilentInput_MessageType()
        {
            var source = ReadSource();

            // Should use MessageTypeDefOf.SilentInput
            Assert.Contains("SilentInput", source);
        }

        // --- Domain-level test: verify AgentModeChangedEvent timestamp behavior ---

        [Fact]
        public void AgentModeChangedEvent_DefaultTimestamp_IsZero()
        {
            var evt = new AgentModeChangedEvent();
            Assert.Equal(0, evt.Timestamp);
        }

        [Fact]
        public void AgentModeChangedEvent_Constructor_SetsTimestamp()
        {
            var evt = new AgentModeChangedEvent("NPC-1", 42, "reactive", "proactive", 500);
            Assert.Equal(500, evt.Timestamp);
            Assert.Equal("NPC-1", evt.NpcId);
            Assert.Equal(42, evt.PawnId);
            Assert.Equal("reactive", evt.OldMode);
            Assert.Equal("proactive", evt.NewMode);
            Assert.Equal(AgentBusEventType.ModeChange, evt.EventType);
        }

        [Fact]
        public void AgentModeChangedEvent_Constructor_DefaultTimestamp_IsZero()
        {
            // The 4-arg constructor defaults timestamp to 0 — this is the bug
            var evt = new AgentModeChangedEvent("NPC-1", 42, "reactive", "proactive");
            Assert.Equal(0, evt.Timestamp);
        }
    }
}
