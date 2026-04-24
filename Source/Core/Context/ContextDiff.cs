using Verse;

namespace RimMind.Core.Context
{
    public class ContextDiff
    {
        public string Key;
        public ContextLayer Layer;
        public string OldValue;
        public string NewValue;
        public int InsertedTick;
        public int RoundsRemaining = 4;

        public string Format()
        {
            return "RimMind.Core.Prompt.StateChange".Translate(Key, OldValue, NewValue);
        }
    }
}
