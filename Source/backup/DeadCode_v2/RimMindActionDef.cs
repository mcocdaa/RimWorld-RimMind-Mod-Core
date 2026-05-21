using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using RimMind.Domain.Enums;

namespace RimMind.Presentation.Agent
{
    public class RimMindActionDef : Def
    {
        public string actionId = "";
        public RiskLevel riskLevel = RiskLevel.Low;
        public float goalProgressDelta = 0.1f;
        public JobDef? jobDef;
        public DutyDef? dutyDef;
        public float needUrgencyWeight = 0f;
        public bool requiresTarget = false;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
                yield return err;
            if (string.IsNullOrEmpty(actionId))
                yield return "RimMindActionDef must have an actionId";
        }
    }
}
