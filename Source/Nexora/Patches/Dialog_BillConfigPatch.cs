using HarmonyLib;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(Dialog_BillConfig))]
public static class Dialog_BillConfigPatch
{
    // 将访问接口添加到工作清单的"完成后放置到{}"
    [HarmonyPatch("FillOutputDropdownOptions")]
    [HarmonyPostfix]
    public static void FillOutputDropdownOptions(Dialog_BillConfig __instance, ref List<FloatMenuOption> opts,
        string prefix,
        Action<ISlotGroup> selected)
    {
        var bill = Traverse.Create(__instance).Field("bill").GetValue<Bill>();
        var map = bill?.billStack.billGiver.Map;
        if (map == null) return;

        var interfaces = map.listerBuildings.AllBuildingsColonistOfClass<Building_AccessInterface>();
        foreach (var @interface in interfaces)
        {
            var proxy = new BillTargetProxy(@interface);
            var label = string.Format(prefix, proxy.SlotYielderLabel());
            var option = new FloatMenuOption(label, () => selected(proxy.GetSlotGroup()));
            opts.Add(option);
        }
    }
}