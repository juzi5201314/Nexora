using HarmonyLib;
using Nexora.buildings;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(ReservationManager))]
public static class ReservationManagerPatch
{
    // 在任务取消时，会释放掉预订的物品，这个时候需要将暂存在访问接口的物品返回到网络
    [HarmonyPatch(nameof(ReservationManager.ReleaseClaimedBy))]
    [HarmonyPrefix]
    public static void ReleaseClaimedBy(List<ReservationManager.Reservation> ___reservations, Pawn claimant, Job job)
    {
        for (var i = ___reservations.Count - 1; i >= 0; i--)
        {
            var reservation = ___reservations[i];
            if (reservation.Claimant == claimant && reservation.Job == job && reservation.Target.HasThing &&
                reservation.Target.Thing.holdingOwner is AccessInterfaceThingOwnerProxy owner)
            {
                var num = reservation.StackCount == -1 ? reservation.Target.Thing.stackCount : reservation.StackCount;
                owner.ReturnToNetwork(reservation.Target.Thing, num);
            }
        }
    }
}