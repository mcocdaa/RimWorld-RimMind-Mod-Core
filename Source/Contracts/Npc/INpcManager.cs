using System.Collections.Generic;

namespace RimMind.Contracts.Npc
{
    public interface INpcManager
    {
        void SpawnNpc(NpcProfile profile);
        void KillNpc(string npcId);
        bool IsNpcAlive(string npcId);
        NpcProfile? GetNpc(string npcId);
        IReadOnlyList<NpcProfile> GetAllNpcs();
        string GetNpcForMap(object map);
        object? FindPawnByNpcId(string npcId);
        object? FindProxyPawnForMap(object map);
        void RegisterActiveAgent(int thingId);
        void UnregisterActiveAgent(int thingId);
        HashSet<int> GetActiveAgentPawnIds();
        void IndexPawn(object pawn);
        void UnindexPawn(int thingId);
        string GetMapNpcId(object map);
    }
}
