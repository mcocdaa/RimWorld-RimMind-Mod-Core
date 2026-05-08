namespace RimMind.Contracts.Context
{
    public interface IPawnDataExtractor
    {
        PawnExtractedData Extract(object pawn);
    }

    public class PawnExtractedData
    {
        public string? MoodString;
        public float MoodPercent;
        public bool HasMap;
        public float Temperature;
    }
}
