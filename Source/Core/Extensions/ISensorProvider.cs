using Verse;

namespace RimMind.Core.Extensions
{
    public interface ISensorProvider
    {
        string SensorId { get; }
        int Priority { get; }
        string? Sense(Pawn pawn);
    }
}
