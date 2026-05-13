using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.Infrastructure.Verse
{
    public sealed class VerseThreadChecker : IThreadChecker
    {
        public bool IsMainThread => global::Verse.UnityData.IsInMainThread;
    }
}
