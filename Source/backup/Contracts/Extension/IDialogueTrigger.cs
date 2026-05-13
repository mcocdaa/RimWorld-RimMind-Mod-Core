namespace RimMind.Contracts.Extension;

public interface IDialogueTrigger : IExtension
{
    void Trigger(object pawn, string context, object? recipient);
}
