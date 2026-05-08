using System.Collections.Generic;
using RimMind.Contracts.Client;

namespace RimMind.Core.Internal
{
    public interface IAIDebugLog
    {
        IReadOnlyList<AIDebugEntry> Entries { get; }
        void Clear();
        void Record(AIRequest request, AIResponse response, int elapsedMs);
    }
}
