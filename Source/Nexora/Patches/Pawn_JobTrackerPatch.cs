using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

// References: https://github.com/Relvl/Rimworld-DigitalStorageUnit/blob/master/Source/_harmony/Patch_Pawn_JobTracker.cs
// 在启动任务时，将目标物品从网络中取出，暂存到访问接口中
[HarmonyPatch(typeof(Pawn_JobTracker))]
public static class Pawn_JobTrackerPatch
{
    [HarmonyPatch(nameof(Pawn_JobTracker.StartJob))]
    [HarmonyPrefix]
    public static void StartJob(Job newJob, Pawn ___pawn, Pawn_JobTracker __instance)
    {
        if (___pawn?.Faction is not { IsPlayer: true }) return;

        var isHaulJobType = newJob.targetA.Thing?.def?.EverStorable(false) ?? false;

        var destPos = isHaulJobType
            ? newJob.targetB.Thing?.Position ?? newJob.targetB.Cell
            : newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
        if (destPos is not { IsValid: true }) destPos = ___pawn.Position;

        var map = (isHaulJobType ? newJob.targetB.Thing?.Map : newJob.targetA.Thing?.Map) ?? ___pawn.Map;

        if ((isHaulJobType && newJob.targetA == null)
            || (!isHaulJobType &&
                (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0) && !newJob.targetB.HasThing)
            || map is null)
        {
            return;
        }

        var network = map.GetComponent<LocalNetwork>();
        var inter = network?.GetClosestAccessInterface(
            destPos,
            newJob.bill?.ingredientSearchRadius ?? float.MaxValue
        );
        if (inter is null) return;

        if (isHaulJobType)
        {
            Log.Message(
                $"start haul job: {newJob}, {newJob.count}, {newJob.targetA.Thing?.LabelCap ?? ""}");
            (newJob.targetA, newJob.count) = ProcessThing(newJob.targetA, newJob.count);
        }
        else
        {
            if (newJob.targetQueueB.NullOrEmpty() || newJob.countQueue.NullOrEmpty())
            {
                Log.Message(
                    $"start bill job: {newJob}, {newJob.count}, {newJob.targetA.IsValid}, {newJob.targetB.Thing?.LabelCap ?? ""}");
                (newJob.targetB, newJob.count) = ProcessThing(newJob.targetB, newJob.count);
            }
            else
            {
                for (var idx = 0; idx < newJob.targetQueueB!.Count; idx++)
                {
                    Log.Message(
                        $"start mu job: {newJob}, {newJob.countQueue[idx]}, {newJob.targetQueueB[idx].Thing.LabelCap}");
                    (newJob.targetQueueB[idx], newJob.countQueue[idx]) =
                        ProcessThing(newJob.targetQueueB[idx], newJob.countQueue[idx]);
                }
            }
        }

        return;

        (LocalTargetInfo, int) ProcessThing(LocalTargetInfo target, int count)
        {
            var item = target.Thing;
            if (item is { holdingOwner: ItemStorage })
            {
                var num = Math.Min(item.stackCount, count == -1 ? item.stackCount : count);
                var other = item.SplitOff(num);
                GenDrop.TryDropSpawn(other, inter.Position, inter.Map, ThingPlaceMode.Direct, out var res);
                inter.InnerThingOwner.AddTempJobTarget(res);
                return (new LocalTargetInfo(res), num);
            }

            return (target, count);
        }
    }
}