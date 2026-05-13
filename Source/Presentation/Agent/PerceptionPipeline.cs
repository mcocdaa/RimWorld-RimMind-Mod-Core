using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Presentation.Agent
{
    public class PerceptionPipeline
    {
        private readonly List<IPerceptionFilter> _filters = new List<IPerceptionFilter>();

        public void AddFilter(IPerceptionFilter filter) => _filters.Add(filter);

        public List<PerceptionBufferEntry> Process(List<PerceptionBufferEntry> entries)
        {
            var result = entries;
            foreach (var filter in _filters)
                result = filter.Apply(result);
            return result;
        }
    }

    public class DedupFilter : IPerceptionFilter
    {
        public List<PerceptionBufferEntry> Apply(List<PerceptionBufferEntry> entries)
        {
            var seen = new HashSet<string>();
            return entries.Where(e =>
            {
                string key = $"{e.PerceptionType}:{e.Content}";
                return seen.Add(key);
            }).ToList();
        }
    }

    public class PriorityFilter : IPerceptionFilter
    {
        public List<PerceptionBufferEntry> Apply(List<PerceptionBufferEntry> entries)
            => entries.OrderByDescending(e => e.Importance).ToList();
    }

    public class CooldownFilter : IPerceptionFilter
    {
        public List<PerceptionBufferEntry> Apply(List<PerceptionBufferEntry> entries)
            => entries;
    }
}
