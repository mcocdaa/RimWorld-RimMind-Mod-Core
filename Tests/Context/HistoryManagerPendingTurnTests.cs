using System.Linq;
using RimMind.Presentation.Context;
using Xunit;

namespace RimMind.Tests.Context
{
    public sealed class HistoryManagerPendingTurnTests
    {
        [Fact]
        public void Pending_turn_is_visible_only_for_display_until_completed_by_stable_id()
        {
            var history = new HistoryManager();

            history.AddPendingTurn(
                "npc-1",
                "turn-42",
                "hello",
                "[[pending:turn-42]]",
                "Dialogue");

            Assert.Empty(history.GetHistory("npc-1", 10));
            Assert.Contains(
                history.GetHistoryForDisplay("npc-1", 10),
                entry => entry.role == "assistant" && entry.content == "[[pending:turn-42]]");
            Assert.DoesNotContain("[[pending:turn-42]]", history.GetAllForSave());

            Assert.True(history.ReplaceAssistantTurn("npc-1", "turn-42", "final answer"));

            var committed = history.GetHistory("npc-1", 10);
            Assert.Equal(new[] { "hello", "final answer" }, committed.Select(entry => entry.content));
            Assert.Contains("final answer", history.GetAllForSave());
            Assert.DoesNotContain("[[pending:turn-42]]", history.GetAllForSave());
        }

        [Fact]
        public void Pending_turn_can_be_removed_by_stable_id_without_touching_other_turns()
        {
            var history = new HistoryManager();
            history.AddPendingTurn("npc-1", "turn-1", "first", "[[pending:1]]", "Dialogue");
            history.AddPendingTurn("npc-1", "turn-2", "second", "[[pending:2]]", "Dialogue");

            Assert.True(history.ReplaceAssistantTurn("npc-1", "turn-1", "first answer"));
            Assert.True(history.RemoveTurn("npc-1", "turn-2"));

            var committed = history.GetHistory("npc-1", 10);
            Assert.Equal(new[] { "first", "first answer" }, committed.Select(entry => entry.content));
            Assert.DoesNotContain("[[pending:2]]", history.GetAllForSave());
        }

        [Fact]
        public void Reused_turn_id_failure_never_removes_or_replaces_completed_history()
        {
            var history = new HistoryManager();
            const string reusedTurnId = "npc-1:process-local-1";

            history.AddPendingTurn("npc-1", reusedTurnId, "old question", "[[old pending]]", "Dialogue");
            Assert.True(history.ReplaceAssistantTurn("npc-1", reusedTurnId, "old completed answer"));

            history.AddPendingTurn("npc-1", reusedTurnId, "new question", "[[new pending]]", "Dialogue");
            Assert.True(history.RemoveTurn("npc-1", reusedTurnId));
            Assert.False(history.ReplaceAssistantTurn("npc-1", reusedTurnId, "must not overwrite completed"));

            var committed = history.GetHistory("npc-1", 10);
            Assert.Equal(
                new[] { "old question", "old completed answer" },
                committed.Select(entry => entry.content));
        }
    }
}
