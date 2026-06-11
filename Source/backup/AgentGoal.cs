using Verse;

namespace RimMind.Application.Common.Models.Agent
{
    public enum GoalStatus
    {
        Proposed,
        Active,
        Achieved,
        Expired,
        Abandoned
    }

    public enum GoalCategory
    {
        Survival,
        Work,
        Social,
        Other
    }

    public class AgentGoal : IExposable
    {
        public string GoalId = "";
        public string Description = "";
        public float Priority;
        public GoalStatus Status = GoalStatus.Proposed;
        public GoalCategory Category = GoalCategory.Other;
        public float Progress;
        public int ExpirationTick;

        public bool IsCompleted => Status == GoalStatus.Achieved;
        public bool IsExpired => ExpirationTick > 0 && Find.TickManager.TicksGame > ExpirationTick;

        public AgentGoal() { }

        public AgentGoal(string description, GoalCategory category, float priority, GoalStatus status)
        {
            GoalId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Description = description;
            Category = category;
            Priority = priority;
            Status = status;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref GoalId, "goalId", "");
            Scribe_Values.Look(ref Description, "description", "");
            Scribe_Values.Look(ref Priority, "priority", 0f);
            Scribe_Values.Look(ref Status, "status", GoalStatus.Proposed);
            Scribe_Values.Look(ref Category, "category", GoalCategory.Other);
            Scribe_Values.Look(ref Progress, "progress", 0f);
            Scribe_Values.Look(ref ExpirationTick, "expirationTick", 0);
        }

        public override string ToString() => $"AgentGoal({Description}, P={Priority:F1}, {Status})";
    }
}
