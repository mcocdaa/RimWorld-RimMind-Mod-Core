using System.Collections.Generic;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Common.Interfaces.Perception
{
    public interface IPerceptionFilter
    {
        List<PerceptionBufferEntry> Apply(List<PerceptionBufferEntry> entries);
    }
}
