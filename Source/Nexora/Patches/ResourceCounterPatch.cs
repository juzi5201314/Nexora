using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
public static class ResourceCounterPatch
{
    public static void Postfix(ResourceCounter __instance, Map ___map, ref Dictionary<ThingDef, int> ___countedAmounts)
    {
        var network = ___map.GetComponent<LocalNetwork>();
        if (network is null) return;

        foreach (var thing in network.GetVirtualItems())
        {
            if (!___countedAmounts.ContainsKey(thing.def))
            {
                ___countedAmounts.Add(thing.def, 0);
            }

            ___countedAmounts[thing.def] += thing.stackCount;
        }
    }
}