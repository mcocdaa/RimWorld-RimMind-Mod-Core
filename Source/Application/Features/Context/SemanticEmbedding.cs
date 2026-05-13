using System;
using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.Application.Features.Context
{
    internal static class SemanticEmbedding
    {
        public static float[] Compute(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<float>();
            var rng = new Random(text.GetHashCode());
            var vector = new float[64];
            for (int i = 0; i < vector.Length; i++)
                vector[i] = (float)(rng.NextDouble() * 2 - 1);
            return vector;
        }

        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0) return 0f;
            float dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            float denom = (float)(Math.Sqrt(na) * Math.Sqrt(nb));
            return denom > 0 ? dot / denom : 0f;
        }
    }
}
