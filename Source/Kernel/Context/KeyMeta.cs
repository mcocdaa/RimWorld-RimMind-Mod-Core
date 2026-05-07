using System;
using System.Collections.Generic;

namespace RimMind.Kernel.Context
{
    public class KeyMeta
    {
        public string Key;
        public ContextLayer Layer;
        public float Priority;
        public Func<object, List<ContextEntry>> ValueProvider;
        public string OwnerMod;
        public bool IsIndexable;
        public float[]? KeyEmbedding;

        public ContextLayer OriginalLayer;
        public int UpdateCount;
        public float AdaptivePriority;
        public float CurrentScore;
        public float CurrentE;

        public KeyMeta(string key, ContextLayer layer, float priority,
            Func<object, List<ContextEntry>> provider, string ownerMod,
            bool isIndexable = false, float[]? keyEmbedding = null)
        {
            Key = key;
            Layer = layer;
            OriginalLayer = layer;
            Priority = priority;
            ValueProvider = provider ?? (_ => new List<ContextEntry>());
            OwnerMod = ownerMod;
            IsIndexable = isIndexable;
            KeyEmbedding = keyEmbedding;
            AdaptivePriority = priority;
        }

        public float GetEffectivePriority()
        {
            return (Priority + AdaptivePriority) / 2f;
        }
    }
}
