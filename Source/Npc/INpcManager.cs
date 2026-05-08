using System.Collections.Generic;
using RimMind.Contracts.Npc;
using Verse;

namespace RimMind.Core.Npc
{
    public interface INpcManager
    {
        void SpawnNpc(NpcProfile profile);
        void KillNpc(string npcId);
        bool IsNpcAlive(string npcId);
        NpcProfile? GetNpc(string npcId);
        IReadOnlyList<NpcProfile> GetAllNpcs();
        string GetNpcForMap(Map map);
        Pawn? FindPawnByNpcId(string npcId);
        Pawn? FindProxyPawnForMap(Map map);
        void RegisterActiveAgent(int thingId);
        void UnregisterActiveAgent(int thingId);
        HashSet<int> GetActiveAgentPawnIds();
        void IndexPawn(Pawn pawn);
        void UnindexPawn(int thingId);
        string GetMapNpcId(Map map);
    }
}
