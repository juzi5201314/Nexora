using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(ItemAvailability))]
public static class ItemAvailabilityPatch
{
    [HarmonyPatch(nameof(ItemAvailability.ThingsAvailableAnywhere))]
    [HarmonyPostfix]
    public static void ThingsAvailableAnywhere(ThingDef need, int amount, Pawn pawn, ref bool __result,
        ref Dictionary<int, bool> ___cachedResults)
    { 
        if (__result)
        {
            return;
        }

        var key = Gen.HashCombine(need.GetHashCode(), pawn.Faction);
        var network = pawn.Map.GetComponent<LocalNetwork>();
        var num = 0;
        foreach (var thing in network.GetItemsByDef(need))
        {
            if (!thing.IsForbidden(pawn))
            {
                num += thing.stackCount;
                if (num >= amount)
                    break;
            }
        }
        var flag = num >= amount;
        ___cachedResults.SetOrAdd(key, flag);
        __result = flag;
    }
}