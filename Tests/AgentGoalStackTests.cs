using System.Collections.Generic;
using System.Linq;
using RimMind.Core.Agent;
using RimMind.Core.AgentBus;
using Verse;
using Xunit;

namespace RimMind.Core.Tests
{
    public class AgentGoalStackTests
    {
        private const int TestPawnId = 42;

        private AgentGoalStack CreateStack()
        {
            return new AgentGoalStack();
        }

        [Fact]
        public void TryAdd_NullGoal_ReturnsFalse()
        {
            var stack = CreateStack();
            bool result = stack.TryAdd(null!, TestPawnId);
            Assert.False(result);
        }

        [Fact]
        public void TryAdd_ValidGoal_ReturnsTrue()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("survive", GoalCategory.Survival, 0.8f, GoalStatus.Proposed);
            bool result = stack.TryAdd(goal, TestPawnId);
            Assert.True(result);
        }

        [Fact]
        public void TryAdd_ProposedGoal_PromotedToActiveWhenSlotAvailable()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("eat", GoalCategory.Survival, 0.8f, GoalStatus.Proposed);
            stack.TryAdd(goal, TestPawnId);
            Assert.Equal(GoalStatus.Active, goal.Status);
            Assert.Equal(1, stack.ActiveCount);
        }

        [Fact]
        public void TryAdd_MultipleGoals_SortedByPriority()
        {
            var stack = CreateStack();
            var low = new AgentGoal("low", GoalCategory.Other, 0.2f, GoalStatus.Proposed);
            var high = new AgentGoal("high", GoalCategory.Survival, 0.9f, GoalStatus.Proposed);
            var mid = new AgentGoal("mid", GoalCategory.Work, 0.5f, GoalStatus.Proposed);

            stack.TryAdd(low, TestPawnId);
            stack.TryAdd(high, TestPawnId);
            stack.TryAdd(mid, TestPawnId);

            Assert.Equal(0.9f, stack.Goals[0].Priority);
            Assert.Equal(0.5f, stack.Goals[1].Priority);
            Assert.Equal(0.2f, stack.Goals[2].Priority);
        }

        [Fact]
        public void TryAdd_MaxActiveGoals_OnlyThreeActive()
        {
            var stack = CreateStack();
            for (int i = 0; i < 5; i++)
            {
                stack.TryAdd(new AgentGoal($"goal_{i}", GoalCategory.Survival, 0.5f + i * 0.1f, GoalStatus.Proposed), TestPawnId);
            }

            Assert.Equal(3, stack.ActiveCount);
            Assert.Equal(5, stack.TotalCount);
        }

        [Fact]
        public void TryAdd_MaxTotalGoals_ReplacesProposed()
        {
            var stack = CreateStack();
            for (int i = 0; i < 10; i++)
            {
                stack.TryAdd(new AgentGoal($"goal_{i}", GoalCategory.Survival, 0.5f, GoalStatus.Proposed), TestPawnId);
            }

            Assert.Equal(10, stack.TotalCount);
            Assert.Equal(3, stack.ActiveCount);

            bool added = stack.TryAdd(new AgentGoal("overflow", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            Assert.True(added);
            Assert.Equal(10, stack.TotalCount);
        }

        [Fact]
        public void TryAdd_MaxTotalGoals_AllSlotsUsedByActive_NoProposed_ReturnsFalse()
        {
            var stack = CreateStack();
            for (int i = 0; i < 10; i++)
            {
                stack.TryAdd(new AgentGoal($"goal_{i}", GoalCategory.Survival, 0.5f, GoalStatus.Proposed), TestPawnId);
            }

            Assert.Equal(10, stack.TotalCount);
            Assert.Equal(3, stack.ActiveCount);

            foreach (var g in stack.Goals.Where(g => g.Status == GoalStatus.Proposed).ToList())
            {
                g.Status = GoalStatus.Active;
            }

            int actualActive = stack.Goals.Count(g => g.Status == GoalStatus.Active);
            Assert.Equal(10, actualActive);

            bool added = stack.TryAdd(new AgentGoal("no_room", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            Assert.False(added);
        }

        [Fact]
        public void Remove_ExistingGoal_ReturnsTrue()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("target", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);

            bool removed = stack.Remove("target", TestPawnId);
            Assert.True(removed);
            Assert.Equal(0, stack.TotalCount);
        }

        [Fact]
        public void Remove_NonexistentGoal_ReturnsFalse()
        {
            var stack = CreateStack();
            bool removed = stack.Remove("nonexistent", TestPawnId);
            Assert.False(removed);
        }

        [Fact]
        public void Remove_ActiveGoal_DecrementsActiveCount()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("active_goal", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            Assert.Equal(1, stack.ActiveCount);

            stack.Remove("active_goal", TestPawnId);
            Assert.Equal(0, stack.ActiveCount);
        }

        [Fact]
        public void Remove_ActiveGoal_PromotesProposedGoal()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g3", GoalCategory.Survival, 0.7f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g4", GoalCategory.Survival, 0.6f, GoalStatus.Proposed), TestPawnId);

            Assert.Equal(3, stack.ActiveCount);
            Assert.Equal(4, stack.TotalCount);

            stack.Remove("g1", TestPawnId);

            Assert.Equal(3, stack.ActiveCount);
            Assert.Equal(3, stack.TotalCount);
            Assert.Equal(GoalStatus.Active, stack.Goals.First(g => g.Description == "g4").Status);
        }

        [Fact]
        public void CheckExpired_RemovesExpiredGoals()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("expiring", GoalCategory.Survival, 0.8f, GoalStatus.Proposed)
            {
                ExpirationTick = 100
            };
            stack.TryAdd(goal, TestPawnId);
            Assert.Equal(1, stack.TotalCount);
            Assert.Equal(1, stack.ActiveCount);

            Find.TickManager.TicksGame = 200;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(0, stack.TotalCount);
            Assert.Equal(0, stack.ActiveCount);
        }

        [Fact]
        public void CheckExpired_DoesNotRemoveUnexpiredGoals()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("not_expired", GoalCategory.Survival, 0.8f, GoalStatus.Proposed)
            {
                ExpirationTick = 999999
            };
            stack.TryAdd(goal, TestPawnId);

            Find.TickManager.TicksGame = 200;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(1, stack.TotalCount);
        }

        [Fact]
        public void CheckExpired_ExpiredActiveGoal_DecrementsActiveCount()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("exp_active", GoalCategory.Survival, 0.8f, GoalStatus.Proposed)
            {
                ExpirationTick = 100
            };
            stack.TryAdd(goal, TestPawnId);
            Assert.Equal(1, stack.ActiveCount);

            Find.TickManager.TicksGame = 200;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(0, stack.ActiveCount);
        }

        [Fact]
        public void CheckExpired_ExpiredGoal_PromotesProposed()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("a1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("a2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("a3", GoalCategory.Survival, 0.7f, GoalStatus.Proposed), TestPawnId);
            var proposed = new AgentGoal("a4", GoalCategory.Survival, 0.6f, GoalStatus.Proposed);
            proposed.ExpirationTick = 0;
            stack.TryAdd(proposed, TestPawnId);

            Assert.Equal(3, stack.ActiveCount);
            Assert.Equal(4, stack.TotalCount);

            Find.TickManager.TicksGame = 100;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(3, stack.ActiveCount);
        }

        [Fact]
        public void Clear_RemovesAllGoals()
        {
            var stack = CreateStack();
            for (int i = 0; i < 5; i++)
                stack.TryAdd(new AgentGoal($"g_{i}", GoalCategory.Survival, 0.5f, GoalStatus.Proposed), TestPawnId);

            Assert.True(stack.TotalCount > 0);

            stack.Clear();

            Assert.Equal(0, stack.TotalCount);
            Assert.Equal(0, stack.ActiveCount);
        }

        [Fact]
        public void ActiveGoals_ReturnsOnlyActiveGoals()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("active1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("active2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);

            var activeGoals = stack.ActiveGoals;
            Assert.All(activeGoals, g => Assert.Equal(GoalStatus.Active, g.Status));
        }

        [Fact]
        public void ActiveGoals_Cached_InvalidatedOnMutation()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            var first = stack.ActiveGoals;
            Assert.Single(first);

            stack.TryAdd(new AgentGoal("g2", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            var second = stack.ActiveGoals;
            Assert.Equal(2, second.Count);
        }

        [Fact]
        public void ActiveCount_StayInSyncWithGoals()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g3", GoalCategory.Survival, 0.7f, GoalStatus.Proposed), TestPawnId);

            int actualActive = 0;
            foreach (var g in stack.Goals)
                if (g.Status == GoalStatus.Active) actualActive++;

            Assert.Equal(actualActive, stack.ActiveCount);
        }

        [Fact]
        public void ActiveCount_AfterRemove_StayInSync()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);

            stack.Remove("g1", TestPawnId);

            int actualActive = 0;
            foreach (var g in stack.Goals)
                if (g.Status == GoalStatus.Active) actualActive++;

            Assert.Equal(actualActive, stack.ActiveCount);
        }

        [Fact]
        public void ActiveCount_AfterCheckExpired_StayInSync()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("exp", GoalCategory.Survival, 0.9f, GoalStatus.Proposed)
            {
                ExpirationTick = 100
            };
            stack.TryAdd(goal, TestPawnId);

            Find.TickManager.TicksGame = 200;
            stack.CheckExpired(TestPawnId);

            int actualActive = 0;
            foreach (var g in stack.Goals)
                if (g.Status == GoalStatus.Active) actualActive++;

            Assert.Equal(actualActive, stack.ActiveCount);
        }

        [Fact]
        public void TryAdd_GoalWithZeroExpirationTick_DoesNotExpireImmediately()
        {
            var stack = CreateStack();
            var goal = new AgentGoal("no_expire", GoalCategory.Survival, 0.8f, GoalStatus.Proposed)
            {
                ExpirationTick = 0
            };
            stack.TryAdd(goal, TestPawnId);

            Find.TickManager.TicksGame = 100000;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(1, stack.TotalCount);
        }

        [Fact]
        public void Goals_ReturnsAllGoals()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("g2", GoalCategory.Work, 0.5f, GoalStatus.Proposed), TestPawnId);

            Assert.Equal(2, stack.Goals.Count);
        }

        [Fact]
        public void TotalCount_ReflectsAllGoals()
        {
            var stack = CreateStack();
            Assert.Equal(0, stack.TotalCount);

            stack.TryAdd(new AgentGoal("g1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            Assert.Equal(1, stack.TotalCount);

            stack.TryAdd(new AgentGoal("g2", GoalCategory.Work, 0.5f, GoalStatus.Proposed), TestPawnId);
            Assert.Equal(2, stack.TotalCount);
        }

        [Fact]
        public void PromoteProposed_FillsActiveSlots()
        {
            var stack = CreateStack();
            stack.TryAdd(new AgentGoal("a1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("a2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("a3", GoalCategory.Survival, 0.7f, GoalStatus.Proposed), TestPawnId);
            stack.TryAdd(new AgentGoal("p1", GoalCategory.Work, 0.6f, GoalStatus.Proposed), TestPawnId);

            Assert.Equal(3, stack.ActiveCount);

            stack.Remove("a1", TestPawnId);

            Assert.Equal(3, stack.ActiveCount);
            Assert.Equal(GoalStatus.Active, stack.Goals.First(g => g.Description == "p1").Status);
        }

        [Fact]
        public void CheckExpired_MultipleExpired_AllRemoved()
        {
            var stack = CreateStack();
            var g1 = new AgentGoal("exp1", GoalCategory.Survival, 0.9f, GoalStatus.Proposed) { ExpirationTick = 100 };
            var g2 = new AgentGoal("exp2", GoalCategory.Survival, 0.8f, GoalStatus.Proposed) { ExpirationTick = 100 };
            stack.TryAdd(g1, TestPawnId);
            stack.TryAdd(g2, TestPawnId);

            Find.TickManager.TicksGame = 200;
            stack.CheckExpired(TestPawnId);

            Assert.Equal(0, stack.TotalCount);
        }
    }
}
