using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core.AgentBus;
using RimMind.Core.Internal;
using Verse;

namespace RimMind.Core.Npc
{
    public class NpcManager : GameComponent
    {
        private ConcurrentDictionary<string, NpcProfile> _registry = new ConcurrentDictionary<string, NpcProfile>();
        private static NpcManager? _instance;
        public static NpcManager? Instance
        {
            get => RimMindServiceLocator.Get<NpcManager>() ?? _instance;
        }

        private static readonly ConcurrentDictionary<int, Pawn> _pawnIndex = new ConcurrentDictionary<int, Pawn>();

        public static void IndexPawn(Pawn pawn)
        {
            if (pawn != null)
                _pawnIndex[pawn.thingIDNumber] = pawn;
        }

        public static void UnindexPawn(int thingId)
        {
            _pawnIndex.TryRemove(thingId, out _);
        }

        internal static void ClearPawnIndex()
        {
            _pawnIndex.Clear();
        }

        public NpcManager(Game game) : base()
        {
            _instance = this;
            RimMindServiceLocator.Register(this);
        }

        public override void LoadedGame()
        {
            _instance = this;
            RimMindServiceLocator.Register(this);
            _pawnIndex.Clear();
        }

        public void SpawnNpc(NpcProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.NpcId)) return;
            _registry[profile.NpcId] = profile;
        }

        public void KillNpc(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            if (_registry.TryRemove(npcId, out var profile))
            {
                AgentBus.AgentBus.Publish(new AgentLifecycleEvent(npcId, 0, "Alive", "Dead"));
            }
        }

        public bool IsNpcAlive(string npcId)
        {
            return !string.IsNullOrEmpty(npcId) && _registry.ContainsKey(npcId);
        }

        public NpcProfile? GetNpc(string npcId)
        {
            return string.IsNullOrEmpty(npcId) ? null
                : _registry.TryGetValue(npcId, out var p) ? p : null;
        }

        public IReadOnlyList<NpcProfile> GetAllNpcs() => _registry.Values.ToList();

        public static string GetMapNpcId(Map map)
        {
            return $"map-{map.uniqueID}";
        }

        public string GetNpcForMap(Map map)
        {
            string mapNpcId = GetMapNpcId(map);
            if (IsNpcAlive(mapNpcId))
                return mapNpcId;
            return "NPC-storyteller";
        }

        public static Pawn? FindPawnByNpcId(string npcId)
        {
            if (string.IsNullOrEmpty(npcId) || !npcId.StartsWith("NPC-")) return null;
            if (!int.TryParse(npcId.Substring(4), out int thingId)) return null;

            if (_pawnIndex.TryGetValue(thingId, out var indexed))
            {
                if (!indexed.DestroyedOrNull() && !indexed.Dead)
                    return indexed;
                _pawnIndex.TryRemove(thingId, out _);
            }

            foreach (var map in Find.Maps)
            {
                if (map?.mapPawns == null) continue;
                var pawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == thingId);
                if (pawn != null)
                {
                    _pawnIndex[thingId] = pawn;
                    return pawn;
                }
            }

            var worldPawn = Find.WorldPawns?.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == thingId);
            if (worldPawn != null)
                _pawnIndex[thingId] = worldPawn;
            return worldPawn;
        }

        public static Pawn? FindProxyPawnForMap(Map map)
        {
            var colonist = map.mapPawns?.FreeColonists?
                .FirstOrDefault(p => p.IsFreeNonSlaveColonist && !p.Dead);
            if (colonist != null) return colonist;
            return map.mapPawns?.AllPawns?
                .FirstOrDefault(p => p.IsFreeNonSlaveColonist && !p.Dead) ?? null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            var dict = new Dictionary<string, NpcProfile>(_registry);
            Scribe_Collections.Look(ref dict, "npcRegistry", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _registry.Clear();
                if (dict != null)
                    foreach (var kv in dict)
                        _registry[kv.Key] = kv.Value;
            }
            _registry ??= new ConcurrentDictionary<string, NpcProfile>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _instance = this;
        }
    }
}
