using RimMind.Contracts.Internal;

namespace RimMind.Core.Registry
{
    public static class AIRequestQueue
    {
        public static IAIRequestQueue? Instance =>
            RimMind.Core.Runtime.RimMindRuntime.Instance?.Queue;

        public static int GetCooldownTicksLeft(string modId)
            => Instance?.GetCooldownTicksLeft(modId) ?? 0;

        public static void ClearCooldown(string modId)
            => Instance?.ClearCooldown(modId);

        public static void ClearAllCooldowns()
            => Instance?.ClearAllCooldowns();
    }
}
