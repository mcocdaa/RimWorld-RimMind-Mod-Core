using RimWorld;
using Verse;

namespace RimMind.Contracts.Extension;

public interface IDialogueTrigger : IExtension
{
    void Trigger(Pawn pawn, string context, Pawn? recipient);
}
