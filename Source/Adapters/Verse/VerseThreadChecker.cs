using RimMind.Kernel.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseThreadChecker : IThreadChecker
    {
        public bool IsMainThread => Verse.UnityData.IsInMainThread;
    }
}
