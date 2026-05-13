using Verse;

namespace RimMind.Presentation.Runtime
{
    public class RuntimeOverrides : IExposable
    {
        public int? OverrideThinkCooldownTicks;
        public int? OverrideAgentTickInterval;
        public int? OverrideMaxToolCallDepth;
        public float? OverrideTemperature;
        public int? OverrideMaxTokens;

        public void ExposeData()
        {
            int? thinkCooldown = OverrideThinkCooldownTicks;
            int? agentTickInterval = OverrideAgentTickInterval;
            int? maxToolCallDepth = OverrideMaxToolCallDepth;
            float? temperature = OverrideTemperature;
            int? maxTokens = OverrideMaxTokens;

            Scribe_Values.Look(ref thinkCooldown, "overrideThinkCooldownTicks");
            Scribe_Values.Look(ref agentTickInterval, "overrideAgentTickInterval");
            Scribe_Values.Look(ref maxToolCallDepth, "overrideMaxToolCallDepth");
            Scribe_Values.Look(ref temperature, "overrideTemperature");
            Scribe_Values.Look(ref maxTokens, "overrideMaxTokens");

            OverrideThinkCooldownTicks = thinkCooldown;
            OverrideAgentTickInterval = agentTickInterval;
            OverrideMaxToolCallDepth = maxToolCallDepth;
            OverrideTemperature = temperature;
            OverrideMaxTokens = maxTokens;
        }
    }
}
