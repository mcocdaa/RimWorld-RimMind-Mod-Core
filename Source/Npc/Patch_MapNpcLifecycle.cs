using HarmonyLib;
using RimMind.Core.Npc;
using Verse;

namespace RimMind.Core.Patch
{
    [HarmonyPatch(typeof(MapGenerator), "GenerateMap")]
    public static class Patch_MapGenerator_GenerateMap
    {
        static void Postfix(Map __result)
        {
            if (__result == null) return;

            var comp = __result.GetComponent<MapNpcComponent>();
            if (comp == null)
            {
                __result.components.Add(new MapNpcComponent(__result));
            }
        }
    }

    [HarmonyPatch(typeof(Map), "Deinit")]
    public static class Patch_Map_Deinit
    {
        static void Prefix(Map __instance)
        {
            if (__instance == null) return;

            string npcId = NpcManager.GetMapNpcId(__instance);
            if (NpcManager.Instance != null && NpcManager.Instance.IsNpcAlive(npcId))
            {
                NpcManager.Instance.KillNpc(npcId);
            }
        }
    }
}
