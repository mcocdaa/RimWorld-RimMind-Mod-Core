using Verse;

namespace RimMind.Core.Npc
{
    public class MapNpcComponent : MapComponent, IExposable
    {
        private NpcProfile _profile = null!;
        private string _npcId = "";

        public NpcProfile Profile => _profile;

        public MapNpcComponent(Map map) : base(map)
        {
            _npcId = NpcManager.GetMapNpcId(map);

            if (NpcManager.Instance != null && !NpcManager.Instance.IsNpcAlive(_npcId))
            {
                _profile = NpcProfileBuilder.BuildMapNpc(map);
                NpcManager.Instance.SpawnNpc(_profile);
            }
            else if (NpcManager.Instance != null)
            {
                _profile = NpcManager.Instance.GetNpc(_npcId);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _npcId, "npcId");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (NpcManager.Instance != null)
                {
                    _profile = NpcManager.Instance.GetNpc(_npcId);
                    if (_profile == null && !string.IsNullOrEmpty(_npcId))
                    {
                        _profile = NpcProfileBuilder.BuildMapNpc(map);
                        NpcManager.Instance.SpawnNpc(_profile);
                    }
                }
            }
        }
    }
}
