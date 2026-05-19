namespace RimMind.Application.Common.Defaults
{
    public sealed class NullDialogueTrigger : RimMind.Application.Common.Interfaces.Extension.IDialogueTrigger
    {
        public static readonly NullDialogueTrigger Instance = new NullDialogueTrigger();

        public string Id => "null.dialogue-trigger";
        public void Trigger(object pawn, string context, object? recipient) { }
    }
}
