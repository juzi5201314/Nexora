using System.Diagnostics;
using HarmonyLib;
using Nexora.network;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(GenClosest))]
public static class GenClosestPatch
{
    // 寻找离指定位置最近的物品
    // 已知在殖民地动物寻找食物会触发
    [HarmonyPatch("ClosestThingReachable")]
    [HarmonyPostfix]
    public static void ClosestThingReachable(ref Thing __result, IntVec3 root,
        Map map,
        ThingRequest thingReq,
        PathEndMode peMode,
        TraverseParms traverseParams,
        float maxDistance = 9999f,
        Predicate<Thing> validator = null)
    {
        var network = map.GetComponent<LocalNetwork>();
        if (network == null) return;

        var things = network.GetItemByRequest(thingReq);

        var inter = network.GetClosestAccessInterface(root, maxDistance, peMode, traverseParams);
        if (inter == null) return;

        if (__result != null && __result.Position.DistanceTo(root) >= inter.Position.DistanceTo(root))
        {
            return;
        }

        var thing = things.FirstOrFallback(thing => validator(thing!), null);
        if (thing == null) return;

        __result = thing;
        Log.Message($"ClosestThingReachable: {__result.LabelCap}");
    }
}