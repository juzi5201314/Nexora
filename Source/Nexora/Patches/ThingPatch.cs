using HarmonyLib;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(Thing))]
public static class ThingPatch
{
    [HarmonyPatch(nameof(Thing.Map), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetMap(Thing __instance, ref Map __result)
    {
        if (__instance.holdingOwner is ItemStorage storage)
        {
            __result = storage.Owner.Map;
            return false;
        }

        return true;
    }

    [HarmonyPatch(nameof(Thing.Position), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetPosition(Thing __instance, ref IntVec3 __result)
    {
        if (__instance.holdingOwner is ItemStorage storage)
        {
            __result = storage.Owner.Position;
            return false;
        }

        return true;
    }
}