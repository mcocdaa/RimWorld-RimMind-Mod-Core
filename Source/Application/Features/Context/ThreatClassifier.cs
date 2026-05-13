using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.Application.Features.Context
{
    public static class ThreatClassifier
    {
        private static readonly HashSet<string> ThreatKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "raid", "infestation", "mech", "toxic", "eclipse", "solar flare",
            "manhunter", "zombie", "siege", "ambush"
        };

        public static bool IsThreat(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var keyword in ThreatKeywords)
            {
                if (text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static float ThreatScore(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            int count = 0;
            foreach (var keyword in ThreatKeywords)
            {
                if (text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    count++;
            }
            return Math.Min(1f, count / 3f);
        }

        public static string ClassifyThreatTier(float wealth, float high, float medium, float low, float threatScale = 1f)
        {
            float adjusted = wealth * threatScale;
            if (adjusted >= high) return "Extreme";
            if (adjusted >= medium) return "High";
            if (adjusted >= low) return "Medium";
            return "Low";
        }
    }
}
