using RimWorld;
using Verse;

namespace RimMind.Contracts.Extension;

public enum SkipCheckKind
{
    Dialogue,
    FloatMenu,
    Action,
    StorytellerIncident
}

public readonly struct SkipCheckArgs
{
    public Pawn? Pawn { get; init; }
    public string? Trigger { get; init; }
    public string? IntentId { get; init; }
}

public interface ISkipCheck : IExtension
{
    SkipCheckKind Kind { get; }
    bool ShouldSkip(in SkipCheckArgs args);
}
