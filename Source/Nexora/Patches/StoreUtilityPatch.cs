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
    [HarmonyPatch(nameof(StoreUtility.TryFindBestBetterStorageFor))]
    [HarmonyPostfix]
    public static void TryFindBestBetterStorageFor(Thing t, ref IHaulDestination haulDestination,
        ref bool __result)
    {
        var h = haulDestination;
        var network = t.MapHeld.GetComponent<LocalNetwork>();
        if (network.Managed(t) && (haulDestination is Building_AccessInterface || network.Storages
                .OfType<Building_ExternalStorageConnector>().Any(b => b.ExternalStorages.Contains(h))))
        {
            haulDestination = null!;
            __result = false;
        }
    }
}