namespace RimMind.Application.Common.Defaults
{
    public sealed class NullModCooldown : RimMind.Application.Common.Interfaces.Extension.IModCooldown
    {
        public static readonly NullModCooldown Instance = new NullModCooldown();

        public string Id => "null.mod-cooldown";
        public int CooldownTicks => 0;
    }
}
