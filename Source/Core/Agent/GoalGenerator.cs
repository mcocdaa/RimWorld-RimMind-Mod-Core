using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Agent
{
    public static class GoalGenerator
    {
        public static List<AgentGoal> GenerateFromIdentity(Pawn pawn)
        {
            var goals = new List<AgentGoal>();
            if (pawn == null) return goals;
            return goals;
        }

        public static List<AgentGoal> GenerateFromState(Pawn pawn)
        {
            var goals = new List<AgentGoal>();
            if (pawn == null) return goals;
            return goals;
        }

        public static List<AgentGoal> GenerateFromEvent(string perceptionType, string content)
        {
            var goals = new List<AgentGoal>();
            if (string.IsNullOrEmpty(perceptionType)) return goals;
            return goals;
        }
    }
}
