using HarmonyLib;
using Nexora.buildings;
using Nexora.network;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(Dialog_BillConfig))]
public static class Dialog_BillConfigPatch
{
    // 将访问接口添加到工作清单的"完成后放置到{}"
    [HarmonyPatch("FillOutputDropdownOptions")]
    [HarmonyPostfix]
    public static void FillOutputDropdownOptions(Dialog_BillConfig __instance, IntVec3 ___billGiverPos,
        ref List<FloatMenuOption> opts,
        string prefix,
        Action<ISlotGroup> selected)
    {
        var bill = Traverse.Create(__instance).Field("bill").GetValue<Bill>();
        var map = bill?.billStack?.billGiver?.Map;
        var network = map?.GetComponent<LocalNetwork>();
        if (bill == null || network == null) return;

        var interfaces = network.GetAccessInterfaces(___billGiverPos)
            .Where(i => i.HaulDestinationEnabled)
            .OrderBy(i => i.Position.DistanceToSquared(___billGiverPos));
        foreach (var @interface in interfaces)
        {
            var proxy = new BillTargetProxy(@interface);
            var label = string.Format(prefix, proxy.SlotYielderLabel());
            var option = new FloatMenuOption(label, () => selected(proxy.GetSlotGroup()));
            opts.Add(option);
        }
    }
}