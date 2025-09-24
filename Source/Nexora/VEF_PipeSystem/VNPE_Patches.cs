using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;
using VNPE;

namespace Nexora.VEF_PipeSystem;

public static class VNPE_Patches
{
    public static bool HasEnoughFeed(Building_NutrientGrinder __instance, ref bool __result)
    {
        var network = __instance.Map.GetComponent<LocalNetwork>();
        var cachedHoppers = Traverse.Create(__instance).Field("cachedHoppers").GetValue<List<Thing>>();
        if (cachedHoppers.Count <= 0)
        {
            __result = false;
            return false;
        }

        var num = 0.0f;
        foreach (var slot in cachedHoppers.Select(t => t.GetSlotGroup()).Where(s => s != null))
        {
            foreach (var item in network.GetAllItems())
            {
                if (slot.Settings.filter.Allows(item) &&
                    Building_NutrientPasteDispenser.IsAcceptableFeedstock(item.def))
                {
                    num += item.stackCount * item.GetStatValue(StatDefOf.Nutrition);
                }

                if (num >= __instance.def.building.nutritionCostPerDispense)
                {
                    __result = true;
                    return false;
                }
            }
        }

        return true;
    }

    public static void FindFeedInAnyHopper(Building_NutrientGrinder __instance, ref Thing __result)
    {
        if (__result != null)
        {
            return;
        }

        var network = __instance.Map.GetComponent<LocalNetwork>();
        var cachedHoppers = Traverse.Create(__instance).Field("cachedHoppers").GetValue<List<Thing>>();
        foreach (var slot in cachedHoppers.Select(t => t.GetSlotGroup()).Where(s => s != null))
        {
            foreach (var item in network.GetAllItems())
            {
                if (slot.Settings.filter.Allows(item) &&
                    Building_NutrientPasteDispenser.IsAcceptableFeedstock(item.def))
                {
                    __result = item;
                    return;
                }
            }
        }
    }
}