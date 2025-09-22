using System.Diagnostics;
using HarmonyLib;
using Nexora.buildings;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(HaulAIUtility))]
public static class HaulAIUtilityPatch
{
    // 如果试图搬运物品到访问接口的格子上，那么改为搬运到访问接口中
    [HarmonyPatch(nameof(HaulAIUtility.HaulToCellStorageJob))]
    [HarmonyPrefix]
    public static bool HaulToCellStorageJob(Pawn p, Thing t, IntVec3 storeCell, ref Job __result)
    {
        if (storeCell.GetEdifice(p.Map) is not Building_AccessInterface inter)
        {
            return true;
        }

        __result = HaulAIUtility.HaulToContainerJob(p, t, inter);
        return false;
    }
}