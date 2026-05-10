using System.Collections.Generic;

namespace RimMind.Contracts.Pipeline
{
    public interface IPerceptionFilter
    {
        List<PerceptionBufferEntry> Apply(List<PerceptionBufferEntry> entries);
    }
}
