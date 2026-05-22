using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Application.Common.Defaults
{
    public sealed class NullModCooldown : IModCooldown
    {
        public static readonly NullModCooldown Instance = new NullModCooldown();

        public string Id => "null.mod-cooldown";
        public string OwnerModId => "RimMindCore";
        public int CooldownTicks => 0;
    }
}
