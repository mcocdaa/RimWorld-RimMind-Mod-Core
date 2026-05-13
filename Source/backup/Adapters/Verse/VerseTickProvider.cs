using RimMind.Contracts.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseTickProvider : ITickProvider
    {
        public int TicksGame => global::Verse.Find.TickManager?.TicksGame ?? 0;
    }
}
