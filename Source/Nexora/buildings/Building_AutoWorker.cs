using System.Text;
using HarmonyLib;
using Nexora.network;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Nexora.buildings;

public class Building_AutoWorker : Building
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    internal Building_WorkTable? billGiver;

    private Effecter? progressBarEffecter;
    private Bill_Production? currentBill = null;
    private Thing? dominant = null;
    private List<Thing> ingredients = [];
    private UnfinishedThing? unfinishedThing = null;
    private float workTotal = 0;
    private float workLeft = 0;
    private int speed = 0;

    private Pawn Worker =
        PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            canGeneratePawnRelations: false,
            allowAddictions: false, allowGay: false, allowFood: false, forceGenerateNewPawn: true,
            fixedBirthName: "Worker", fixedLastName: "emm"));

    private Job workJob = new(JobDefOf.DoBill);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        LinkWorkTable();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (currentBill != null)
        {
            DoneBill(true);
        }

        Map.reservationManager.Release(billGiver, Worker, workJob);
        base.DeSpawn(mode);
    }

    protected override void Tick()
    {
        base.Tick();
        if (!this.IsHashIntervalTick(60)) return;
        if (billGiver != null)
        {
            if (currentBill != null)
            {
                PollBill();
            }
            else if (billGiver.BillStack.FirstShouldDoNow != null)
            {
                TryStartBill(billGiver.BillStack.FirstShouldDoNow);
            }
        }
        else
        {
            LinkWorkTable();
        }
    }

    public override string GetInspectString()
    {
        var sb = new StringBuilder(base.GetInspectString());
        if (currentBill != null)
        {
            sb.AppendLine(
                $"Working: {JobUtility.GetResolvedJobReport(currentBill.recipe.jobString, billGiver, ingredients.Count > 0 ? ingredients[0] : LocalTargetInfo.Invalid)}");
            sb.AppendLine($"Work left: {workLeft} / {workTotal}");
            sb.AppendLine($"Ingredients: {ingredients.Select(t => t.LabelCap).Join()}");
        }

        return sb.ToString().TrimEnd();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        if (currentBill != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "Cancel".Translate(),
                icon = TexButton.CloseXBig,
                action = () => DoneBill(true),
            };
        }
    }

    public static Building_WorkTable? FindBillGiver(IntVec3 pos, Map map)
    {
        foreach (var vec3 in GenAdj.AdjacentCellsAndInside)
        {
            var cell = pos + vec3;
            if (!cell.InBounds(map)) continue;

            var thingList = cell.GetThingList(map);
            foreach (var t in thingList)
            {
                if (t is Building_WorkTable giver and not Building_WorkTableAutonomous && t.InteractionCell == pos)
                {
                    return giver;
                }
            }
        }

        return null;
    }

    private void LinkWorkTable()
    {
        var table = FindBillGiver(Position, Map);
        if (table != null)
        {
            billGiver = table;
            var reservation1 = new ReservationManager.Reservation(Worker, workJob, 1, -1, table, null);
            Traverse.Create(Map.reservationManager).Field("reservations")
                .GetValue<List<ReservationManager.Reservation>>().Add(reservation1);
            Map.events.Notify_ReservationAdded(reservation1);
        }
    }

    private void TryStartBill(Bill bill)
    {
        if (bill is not Bill_Production billProduction)
        {
            Log.Warning($"no production bill type: {bill.GetType()}");
            return;
        }

        if (!utils.Recipe.TryFindAndTakeBestBillIngredients(billProduction, Network, out var ingres, out _))
        {
            return;
        }

        dominant = utils.Recipe.GetDominantIngredient(bill.recipe, ingres);
        if (bill.recipe.UsesUnfinishedThing)
        {
            var unfThing = (UnfinishedThing)ThingMaker.MakeThing(bill.recipe.unfinishedThingDef,
                bill.recipe.unfinishedThingDef.MadeFromStuff ? dominant?.def : null);
            unfThing.Creator = Worker;
            unfThing.BoundBill = (Bill_ProductionWithUft)bill;
            unfThing.ingredients = ingres;
            unfThing.workLeft = bill.GetWorkAmount(unfThing);
            unfThing.TryGetComp<CompColorable>()?.SetColor(dominant?.DrawColor ?? Color.white);
            unfinishedThing = unfThing;
            workLeft = unfinishedThing.workLeft;
        }
        else
        {
            workLeft = bill.GetWorkAmount();
        }

        workTotal = workLeft;
        speed = (int)((int)workLeft * 0.3f);
        currentBill = billProduction;
        ingredients = ingres;
        bill.SetPawnRestriction(Worker);
        bill.Notify_BillWorkStarted(null);

        progressBarEffecter = EffecterDefOf.ProgressBarAlwaysVisible.SpawnAttached(this, Map);
    }

    private void PollBill()
    {
        if (currentBill == null)
        {
            return;
        }

        workLeft -= speed;
        if (workLeft <= 0)
        {
            DoneBill();
        }

        if (progressBarEffecter != null)
        {
            var progress = Mathf.Clamp01(1 - workLeft / workTotal);
            if (progressBarEffecter.children.Count > 0)
            {
                if (progressBarEffecter.children[0] is SubEffecter_ProgressBar subEffecter)
                {
                    var mote = subEffecter.mote;
                    if (mote == null)
                    {
                        progressBarEffecter.EffectTick(new TargetInfo(this), TargetInfo.Invalid);
                        mote = subEffecter.mote;
                    }

                    if (mote == null) return;
                    mote.progress = progress;
                    mote.alwaysShow = true;
                    var heightOffset = (def.size.z - 1) / 2;
                    mote.offsetZ = 0.5f + heightOffset;
                    progressBarEffecter.EffectTick(new TargetInfo(this), TargetInfo.Invalid);
                }
            }
        }
    }

    private void DoneBill(bool cancel = false)
    {
        if (currentBill == null)
        {
            return;
        }

        if (unfinishedThing != null)
        {
            unfinishedThing.Destroy();
            unfinishedThing = null;
        }

        if (progressBarEffecter != null)
        {
            progressBarEffecter.Cleanup();
            progressBarEffecter = null;
        }

        if (!cancel)
        {
            currentBill.Notify_BillWorkFinished(null);

            ThingStyleDef? style = null;
            if (ModsConfig.IdeologyActive && currentBill.recipe.products != null &&
                currentBill.recipe.products.Count == 1)
                style = currentBill.globalStyle
                    ? Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(currentBill.recipe.ProducedThingDef)
                        ?.styleDef
                    : currentBill.style;
            var products = utils.Recipe.MakeRecipeProducts(currentBill.recipe, Worker, ingredients, dominant,
                billGiver!,
                currentBill.precept, style, currentBill.graphicIndexOverride).ToList<Thing>();

            ingredients.ForEach(t => currentBill.recipe.Worker.ConsumeIngredient(t, currentBill.recipe, Map));
            products.ForEach(t => Network.AddItemOrOverflow(t, Position));
            if (currentBill.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                Map.resourceCounter.UpdateResourceCounts();
            }

            currentBill.Notify_IterationCompleted(null, ingredients);
        }

        if (cancel)
        {
            foreach (var thing in ingredients.Where(thing => !thing.Destroyed))
            {
                Network.AddItemOrOverflow(thing, Position);
            }
        }

        currentBill.SetAnySlaveRestriction();
        dominant = null;
        currentBill = null;
        ingredients.Clear();
    }
}