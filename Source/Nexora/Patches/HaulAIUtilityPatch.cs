using System.Diagnostics;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(HaulAIUtility))]
public static class HaulAIUtilityPatch
{
    [HarmonyPatch(nameof(HaulAIUtility.PawnCanAutomaticallyHaulFast))]
    [HarmonyPostfix]
    public static void PawnCanAutomaticallyHaulFast(Thing t, bool __result)
    {
        if (t.holdingOwner is ItemStorage)
        {
            Log.Message($"PawnCanAutomaticallyHaulFast: {t.LabelCap}: {__result}");
        }
    }

    [HarmonyPatch(nameof(HaulAIUtility.FindFixedIngredientCount))]
    [HarmonyPostfix]
    public static void FindFixedIngredientCount(ThingDef def, int maxCount)
    {
        Log.Message($"FindFixedIngredientCount: {def.LabelCap}: count: {maxCount}");
    }
}
