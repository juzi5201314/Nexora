using Nexora.buildings;
using RimWorld;
using Verse;

namespace Nexora;

public class PlaceWorker_OnInteractionCell : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        if (Building_AutoWorker.FindBillGiver(loc, map) != null)
        {
            return true;
        }
        
        return new AcceptanceReport("必须放置在工作台的操作点上。");
    }
}