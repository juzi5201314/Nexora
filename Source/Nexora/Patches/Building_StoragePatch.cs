using HarmonyLib;
using Nexora.buildings;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(Building_Storage))]
public static class Building_StoragePatch
{
    [HarmonyPatch(nameof(Building_Storage.Notify_ReceivedThing))]
    [HarmonyPostfix]
    public static void Notify_ReceivedThing(Thing newItem, Building_Storage __instance)
    {
        var network = __instance.Map.GetComponent<LocalNetwork>();
        if (network.Storages.OfType<Building_ExternalStorageConnector>()
            .Any(b => b.ExternalStorages.Contains(__instance)))
        {
            newItem.holdingOwner = new EmptyThingOwner(__instance);
        }
    }

    [HarmonyPatch(nameof(Building_Storage.Notify_LostThing))]
    [HarmonyPostfix]
    public static void Notify_LostThing(Thing newItem)
    {
        if (newItem.holdingOwner is EmptyThingOwner)
        {
            newItem.holdingOwner = null;
        }
    }
}