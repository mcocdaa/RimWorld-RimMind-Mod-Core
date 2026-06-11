using System.Collections.Generic;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.UI.AgentsPage;
using Xunit;

namespace RimMind.Tests.Presentation.UI
{
    public class P7D_AgentListBuilderTests
    {
        [Fact]
        public void Build_Groups_Active_And_Paused_Agents()
        {
            var rows = AgentListBuilder.Build(
                new[]
                {
                    AgentListItem.ExistingPawn("pawn-a", "Alice", AgentState.Active),
                    AgentListItem.ExistingPawn("pawn-b", "Bob", AgentState.Paused)
                },
                pendingSelectedPawnId: null,
                pendingSelectedPawnLabel: null);

            Assert.Single(rows.Active);
            Assert.Single(rows.Paused);
            Assert.Empty(rows.PendingCreation);
            Assert.Equal("Alice", rows.Active[0].Label);
            Assert.Equal("Bob", rows.Paused[0].Label);
        }

        [Fact]
        public void Build_Adds_Pending_Creation_For_Selected_Pawn_Without_Agent()
        {
            var rows = AgentListBuilder.Build(
                existingAgents: new List<AgentListItem>(),
                pendingSelectedPawnId: "pawn-42",
                pendingSelectedPawnLabel: "Mira");

            Assert.Empty(rows.Active);
            Assert.Empty(rows.Paused);
            Assert.Single(rows.PendingCreation);
            Assert.Equal("pawn-42", rows.PendingCreation[0].Id);
            Assert.True(rows.PendingCreation[0].IsPendingCreation);
        }

        [Fact]
        public void Build_Does_Not_Add_Pending_When_Pawn_Already_Has_Agent()
        {
            var rows = AgentListBuilder.Build(
                new[]
                {
                    AgentListItem.ExistingPawn("pawn-42", "Mira", AgentState.Active)
                },
                pendingSelectedPawnId: "pawn-42",
                pendingSelectedPawnLabel: "Mira");

            Assert.Single(rows.Active);
            Assert.Empty(rows.PendingCreation);
        }

        [Fact]
        public void Build_Does_Not_Add_Pending_When_Id_Is_Null_Or_Empty()
        {
            var rows1 = AgentListBuilder.Build(
                new List<AgentListItem>(),
                pendingSelectedPawnId: null,
                pendingSelectedPawnLabel: null);

            var rows2 = AgentListBuilder.Build(
                new List<AgentListItem>(),
                pendingSelectedPawnId: "",
                pendingSelectedPawnLabel: "");

            Assert.Empty(rows1.PendingCreation);
            Assert.Empty(rows2.PendingCreation);
        }

        [Fact]
        public void Build_Groups_Other_States_Into_Other()
        {
            var rows = AgentListBuilder.Build(
                new[]
                {
                    AgentListItem.ExistingPawn("pawn-d", "Dormant", AgentState.Dormant),
                    AgentListItem.ExistingPawn("pawn-t", "Terminated", AgentState.Terminated)
                },
                pendingSelectedPawnId: null,
                pendingSelectedPawnLabel: null);

            Assert.Empty(rows.Active);
            Assert.Empty(rows.Paused);
            Assert.Equal(2, rows.Other.Count);
        }

        [Fact]
        public void ExistingPawn_Is_Not_PendingCreation()
        {
            var item = AgentListItem.ExistingPawn("id", "Label", AgentState.Active);
            Assert.False(item.IsPendingCreation);
        }

        [Fact]
        public void PendingPawn_Is_PendingCreation_With_Dormant_State()
        {
            var item = AgentListItem.PendingPawn("id", "Label");
            Assert.True(item.IsPendingCreation);
            Assert.Equal(AgentState.Dormant, item.State);
        }

        [Fact]
        public void Build_Handles_Null_ExistingAgents()
        {
            var rows = AgentListBuilder.Build(
                existingAgents: null,
                pendingSelectedPawnId: null,
                pendingSelectedPawnLabel: null);

            Assert.Empty(rows.Active);
            Assert.Empty(rows.Paused);
            Assert.Empty(rows.PendingCreation);
            Assert.Empty(rows.Other);
        }
    }

    public class P7D_StateBadgeColorTests
    {
        [Fact]
        public void GetStateBadgeColors_Active_Returns_Active_Colors()
        {
            var (text, bg) = RimMind.Infrastructure.UI.RimMindUITheme.GetStateBadgeColors(
                AgentState.Active);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorActive, text);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorBadgeActiveBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_Paused_Returns_Paused_Colors()
        {
            var (text, bg) = RimMind.Infrastructure.UI.RimMindUITheme.GetStateBadgeColors(
                AgentState.Paused);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorPaused, text);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorBadgePausedBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_Terminated_Returns_Terminated_Colors()
        {
            var (text, bg) = RimMind.Infrastructure.UI.RimMindUITheme.GetStateBadgeColors(
                AgentState.Terminated);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorTerminated, text);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorBadgeTerminatedBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_Dormant_Returns_Idle_Colors()
        {
            var (text, bg) = RimMind.Infrastructure.UI.RimMindUITheme.GetStateBadgeColors(
                AgentState.Dormant);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorIdle, text);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorBadgeIdleBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_PendingCreation_Returns_Pending_Colors()
        {
            var (text, bg) = RimMind.Infrastructure.UI.RimMindUITheme.GetStateBadgeColors(
                AgentState.Dormant, isPendingCreation: true);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorPending, text);
            Assert.Equal(RimMind.Infrastructure.UI.RimMindUITheme.ColorBadgePendingBg, bg);
        }
    }
}
