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

    // 使得在虚拟存储中的食物冷藏
    [HarmonyPatch(nameof(ThingOwnerUtility.TryGetFixedTemperature))]
    [HarmonyPrefix]
    public static bool TryGetFixedTemperature(IThingHolder holder, Thing forThing, ref float temperature,
        ref bool __result)
    {
        if (holder is Building_LocalStorage)
        {
            temperature = float.MinValue;
            __result = true;
            return false;
        }

        return true;
    }
}