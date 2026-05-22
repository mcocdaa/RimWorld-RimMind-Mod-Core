using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Application.Common.Defaults
{
    public sealed class NullDialogueTrigger : IDialogueTrigger
    {
        public static readonly NullDialogueTrigger Instance = new NullDialogueTrigger();

        public string Id => "null.dialogue-trigger";
        public string OwnerModId => "RimMindCore";
        public void Trigger(object pawn, string context, object? recipient) { }
    }
}
