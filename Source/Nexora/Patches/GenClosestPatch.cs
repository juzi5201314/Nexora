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

        var things = network.SortedStorages.SelectMany(storage =>
        {
            if (thingReq.singleDef != null)
            {
                return storage.IndexTable.TryGetValue(thingReq.singleDef, out var things)
                    ? things.Keys.AsEnumerable()
                    : [];
            }
            else
            {
                return storage.IndexTable.Where(pair =>
                    thingReq.group.Includes(pair.Key)
                ).SelectMany(pair => pair.Value.Keys.AsEnumerable());
            }
        }).AsEnumerable();

        var inter = network.GetClosestAccessInterface(root, maxDistance);
        if (inter == null) return;
        if (!map.reachability.CanReach(root, inter, peMode, traverseParams)) return;

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