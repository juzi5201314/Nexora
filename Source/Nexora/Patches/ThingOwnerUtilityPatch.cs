using HarmonyLib;
using Nexora.buildings;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(ThingOwnerUtility))]
public static class ThingOwnerUtilityPatch
{
    // 这是为了允许访问接口在`HaulAIUtility.HaulToStorageJob`中被判定为搬运目标
    [HarmonyPatch(nameof(ThingOwnerUtility.TryGetInnerInteractableThingOwner))]
    [HarmonyPrefix]
    public static bool TryGetInnerInteractableThingOwner(this Thing thing, ref ThingOwner __result)
    {
        if (thing is not Building_AccessInterface accessInterface) return true;
        __result = accessInterface.InnerThingOwner;
        return false;
    }
}