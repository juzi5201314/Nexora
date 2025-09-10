using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(JobDriver_HaulToContainer))]
public static class JobDriver_HaulToContainerPatch
{
    // 运行同时搬运多个物品到访问接口
    // 删除了预订访问接口的代码。但是patch整个方法或许不是好主意，也许应该patch Reserve和ReserveAsManyAsPossible ？
    [HarmonyPatch("TryMakePreToilReservations")]
    [HarmonyPrefix]
    public static bool TryMakePreToilReservations(bool errorOnFailed, ref bool __result,
        JobDriver_HaulToContainer __instance)
    {
        var target = __instance.Container;
        if (target is Building_AccessInterface)
        {
            if (!__instance.pawn.Reserve(__instance.job.GetTarget(TargetIndex.A), __instance.job,
                    errorOnFailed: errorOnFailed))
            {
                __result = false;
                return false;
            }

            Traverse.Create(__instance).Method("UpdateEnrouteTrackers").GetValue();
            __instance.pawn.ReserveAsManyAsPossible(__instance.job.GetTargetQueue(TargetIndex.A), __instance.job);
            __result = true;
            return false;
        }

        return true;
    }
}