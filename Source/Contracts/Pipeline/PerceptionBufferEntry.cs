using Verse;

namespace RimMind.Contracts.Pipeline
{
    public class PerceptionBufferEntry : IExposable
    {
        public string PerceptionType = "";
        public string Content = "";
        public float Importance;
        public int Timestamp;
        public int PawnId;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PerceptionType, "perceptionType", "");
            Scribe_Values.Look(ref Content, "content", "");
            Scribe_Values.Look(ref Importance, "importance", 0f);
            Scribe_Values.Look(ref Timestamp, "timestamp", 0);
            Scribe_Values.Look(ref PawnId, "pawnId", 0);
        }
    }
}
