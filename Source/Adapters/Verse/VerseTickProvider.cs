using RimMind.Kernel.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseTickProvider : ITickProvider
    {
        public int TicksGame => Verse.Find.TickManager?.TicksGame ?? 0;
    }
}
