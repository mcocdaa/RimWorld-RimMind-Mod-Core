using Verse;
using RimMind.Domain.Enums;

namespace RimMind.Presentation.Agent
{
    public class RimMindGoalCategoryDef : Def
    {
        public float defaultPriority = 1f;
        public float decayRate = 0.001f;
        public int expiryTicks = 60000;
    }
}
