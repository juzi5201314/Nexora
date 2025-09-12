using HarmonyLib;
using Nexora.buildings;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(StoreUtility))]
public static class StoreUtilityPatch
{
    // 不允许将Nexora存储中的物品移动到Nexora访问接口。这会造成无限循环
    [HarmonyPatch(nameof(StoreUtility.TryFindBestBetterNonSlotGroupStorageFor))]
    [HarmonyPostfix]
    public static void TryFindBestBetterNonSlotGroupStorageFor(Thing t, ref IHaulDestination haulDestination,
        ref bool __result)
    {
        var network = t.Map.GetComponent<LocalNetwork>();
        if (network.Managed(t) && haulDestination is Building_AccessInterface)
        {
            haulDestination = null!;
            __result = false;
        }
    }
}