using HarmonyLib;
using Nexora.buildings;
using Nexora.network;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(Reachability))]
public static class ReachabilityPatch
{
    // 这是为了在存储器不可达时，仍然可以在访问接口访问物品。
    // 在存储器不可达时，如果存在可达的访问接口，那么则改为可达。
    [HarmonyPatch("CanReach", typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
    [HarmonyPostfix]
    public static void CanReach(Reachability __instance, ref bool __result, IntVec3 start, LocalTargetInfo dest,
        PathEndMode peMode, TraverseParms traverseParams)
    {
        if (__result)
        {
            return;
        }

        LocalNetwork network;
        if (dest.Thing is not Building_LocalStorage storage)
        {
            if (dest.HasThing && dest.Thing.holdingOwner is EmptyThingOwner owner)
            {
                network = owner.Storage.Map.GetComponent<LocalNetwork>();
            }
            else
            {
                return;
            }
        }
        else network = storage.Network;

        var inter = network.GetClosestAccessInterface(start, pathEndMode: peMode,
            traverseParams: traverseParams);

        if (inter != null && __instance.CanReach(start, inter, peMode, traverseParams))
        {
            __result = true;
        }
    }
}