using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.Patches;

[HarmonyPatch(typeof(JobDriver_HaulToTransporter))]
public static class JobDriver_HaulToTransporterPatch
{
    // 在搬运到运输舱job开始后，将物品移动到访问接口
    [HarmonyPatch(nameof(JobDriver_HaulToTransporter.Notify_Starting))]
    [HarmonyPostfix]
    public static void Notify_Starting(JobDriver_HaulToTransporter __instance)
    {
        if (!__instance.job.targetA.IsValid ||
            __instance.job.targetA.Thing?.holdingOwner is not ItemStorage storage) return;

        var inter = storage.Network().GetClosestAccessInterface(__instance.job.targetB.Cell,
            traverseParams: TraverseParms.For(__instance.pawn));
        if (inter is null)
        {
            return;
        }

        var thing = __instance.job.targetA.Thing;
        var num = Math.Min(thing.stackCount, __instance.initialCount);
        var other = thing.SplitOff(num).TryMakeMinified();
        if (other.def.destroyOnDrop)
        {
            Log.Warning(
                $"{other.LabelCap} cannot drop. because destroyOnDrop=true");
        }
        else
        {
            GenDrop.TryDropSpawn(other, inter.Position, inter.Map, ThingPlaceMode.Direct, out other);
        }

        if (other != null)
        {
            inter.InnerThingOwner.AddTempThing(other);
            __instance.job.targetA = new LocalTargetInfo(other);
            __instance.job.count = other.stackCount;
            __instance.initialCount = other.stackCount;
        }
    }
}