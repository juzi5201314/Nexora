using System.Diagnostics;
using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(TradeUtility))]
public static class TradeUtilityPatch
{
    // 将网络中的thing加入可交易物品
    [HarmonyPatch(nameof(TradeUtility.AllLaunchableThingsForTrade))]
    [HarmonyPostfix]
    public static IEnumerable<Thing> AllLaunchableThingsForTrade(IEnumerable<Thing> __result, Map map)
    {
        var network = map.GetComponent<LocalNetwork>();
        if (network == null)
        {
            yield break;
        }

        var yieldedThings = new HashSet<Thing>();
        foreach (var thing in __result)
        {
            if (yieldedThings.Add(thing))
            {
                yield return thing;
            }
        }

        foreach (var thing in network.GetVirtualItems())
        {
            if (yieldedThings.Add(thing))
            {
                yield return thing;
            }
        }
    }
}

[HarmonyPatch(typeof(TradeDeal))]
public static class TradeDealPatch
{
    // 让在ItemStorage中的thing可以被出售
    [HarmonyPatch("InSellablePosition")]
    [HarmonyPrefix]
    public static bool InSellablePosition(Thing t, ref bool __result)
    {
        if (t.holdingOwner is ItemStorage)
        {
            __result = true;
            return false;
        }

        return true;
    }
}