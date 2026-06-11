using Verse;

namespace RimMind.Application.Common.Models.Agent
{
    public class BehaviorRecord : IExposable
    {
        public string Action = "";
        public string Reason = "";
        public bool Success;
        public string ResultReason = "";
        public float GoalProgressDelta;
        public int Timestamp;
        public string ActionEventId = "";
        public int DurationMs;

        public BehaviorRecord() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Action, "action", "");
            Scribe_Values.Look(ref Reason, "reason", "");
            Scribe_Values.Look(ref Success, "success", false);
            Scribe_Values.Look(ref ResultReason, "resultReason", "");
            Scribe_Values.Look(ref GoalProgressDelta, "goalProgressDelta", 0f);
            Scribe_Values.Look(ref Timestamp, "timestamp", 0);
            Scribe_Values.Look(ref ActionEventId, "actionEventId", "");
            Scribe_Values.Look(ref DurationMs, "durationMs", 0);
        }

        public override string ToString() => $"BehaviorRecord({Action}, Success={Success}, T={Timestamp})";
    }
}
