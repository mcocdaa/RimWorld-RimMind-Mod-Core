namespace RimMind.Contracts.Extensions;

public interface IDialogueTrigger : IExtension
{
    void Trigger(object pawn, string context, object? recipient);
}
