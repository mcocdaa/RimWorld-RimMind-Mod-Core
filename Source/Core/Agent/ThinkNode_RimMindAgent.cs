using RimMind.Core.Comps;
using Verse;
using Verse.AI;

namespace RimMind.Core.Agent
{
    public class ThinkNode_RimMindAgent : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            var comp = pawn.GetComp<CompPawnAgent>();
            if (comp == null || comp.Agent == null || !comp.Agent.IsActive) return 0f;
            return priority > 0f ? priority : 5f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            var comp = pawn.GetComp<CompPawnAgent>();
            if (comp == null || comp.Agent == null || !comp.Agent.IsActive) return ThinkResult.NoJob;
            var job = comp.Agent.ConsumePendingJob();
            if (job == null) return ThinkResult.NoJob;
            return new ThinkResult(job, this, default, false);
        }
    }
}
