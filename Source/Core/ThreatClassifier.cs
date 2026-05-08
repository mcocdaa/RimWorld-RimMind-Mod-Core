namespace RimMind.Core
{
    public static class ThreatClassifier
    {
        public static string ClassifyThreatTier(float wealth, float high, float medium, float low, float threatScale)
        {
            float adjusted = wealth * threatScale;
            if (adjusted > 100000) return "Extreme";
            if (adjusted > 30000) return "High";
            if (adjusted > 10000) return "Medium";
            return "Low";
        }
    }
}
