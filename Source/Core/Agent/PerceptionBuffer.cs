using System.Collections.Generic;

namespace RimMind.Core.Agent
{
    public class PerceptionBufferEntry
    {
        public string PerceptionType { get; set; } = "";
        public string Content { get; set; } = "";
        public float Importance { get; set; }
        public int Timestamp { get; set; }
        public int PawnId { get; set; }

        public string DedupKey => $"{PerceptionType}:{Content}";
    }

    public class PerceptionBuffer
    {
        private const int DefaultCapacity = 20;
        private readonly List<PerceptionBufferEntry> _entries = new List<PerceptionBufferEntry>();

        public int Capacity { get; }
        public IReadOnlyList<PerceptionBufferEntry> Entries => _entries;

        public PerceptionBuffer(int capacity = DefaultCapacity)
        {
            Capacity = capacity;
        }

        public void Add(PerceptionBufferEntry entry)
        {
            _entries.Add(entry);
            while (_entries.Count > Capacity)
                _entries.RemoveAt(0);
        }

        public List<PerceptionBufferEntry> Flush()
        {
            var result = new List<PerceptionBufferEntry>(_entries);
            _entries.Clear();
            return result;
        }

        public void Clear() => _entries.Clear();
    }
}
