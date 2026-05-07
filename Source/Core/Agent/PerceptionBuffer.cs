using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Agent
{
    public class PerceptionBufferEntry : IExposable
    {
        public string PerceptionType = "";
        public string Content = "";
        public float Importance;
        public int Timestamp;
        public int PawnId;

        public void ExposeData()
        {
            Scribe_Values.Look(ref PerceptionType, "perceptionType", "");
            Scribe_Values.Look(ref Content, "content", "");
            Scribe_Values.Look(ref Importance, "importance", 0f);
            Scribe_Values.Look(ref Timestamp, "timestamp", 0);
            Scribe_Values.Look(ref PawnId, "pawnId", 0);
        }
    }

    public class PerceptionBuffer
    {
        private readonly List<PerceptionBufferEntry> _entries = new List<PerceptionBufferEntry>();
        private const int MaxCapacity = 200;

        public IReadOnlyList<PerceptionBufferEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Add(PerceptionBufferEntry entry)
        {
            if (entry == null) return;
            _entries.Add(entry);
            while (_entries.Count > MaxCapacity)
                _entries.RemoveAt(0);
        }

        public List<PerceptionBufferEntry> Flush()
        {
            var result = new List<PerceptionBufferEntry>(_entries);
            _entries.Clear();
            return result;
        }

        public IReadOnlyList<PerceptionBufferEntry> Get()
        {
            return _entries;
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
