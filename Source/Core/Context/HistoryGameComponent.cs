using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Context
{
    public class HistoryGameComponent : GameComponent
    {
        private Dictionary<string, List<HistoryEntry>> _histories = new Dictionary<string, List<HistoryEntry>>();

        public HistoryGameComponent() : base() { }
        public HistoryGameComponent(Game game) : base() { }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
                _histories = HistoryManager.Instance.GetAllForSave();

            Scribe_Collections.Look(ref _histories, "contextHistories",
                LookMode.Value, LookMode.Deep);
            _histories ??= new Dictionary<string, List<HistoryEntry>>();

            if (Scribe.mode == LoadSaveMode.LoadingVars)
                HistoryManager.Instance.LoadFromSave(_histories);
        }
    }
}
