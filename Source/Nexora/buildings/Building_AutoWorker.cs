using System.Text;
using HarmonyLib;
using Nexora.network;
using Nexora.ui;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Nexora.buildings;

public class Building_AutoWorker : Building
{
    private const int BASE_TARGET_WORKRATE = 60;

    public LocalNetwork Network { get; set; }

    internal Building_WorkTable? billGiver;

    private Effecter? progressBarEffecter;
    private Bill_Production? currentBill = null;
    private Thing? dominant = null;
    private List<Thing> ingredients = [];
    private UnfinishedThing? unfinishedThing = null;
    private float workTotal = 0;
    private float workLeft = 0;
    private float speedFactor = 0;
    private string WorkingStatusStr = "";
    private DynWorkRate? dynWorkRate = null;

    private bool paused = false;
    private int overclocking = 0;
    private int overclockingLimit = 5;
    private int priority = 0;


    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Find.World.GetComponent<StaticSaveData>();
        Network = Map.GetComponent<LocalNetwork>();
        LinkWorkTable();
        Map.events.BuildingDespawned += (building) =>
        {
            if (building == billGiver)
            {
                Map.reservationManager.Release(billGiver, StaticSaveData.Worker, StaticSaveData.workJob);
                DoneBill(true);
            }
        };
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (currentBill != null)
        {
            DoneBill(true);
        }

        if (dynWorkRate != null)
        {
            Network.ReleaseWorkRate(dynWorkRate);
        }

        if (billGiver != null)
        {
            Map.reservationManager.Release(billGiver, StaticSaveData.Worker, StaticSaveData.workJob);
        }

        base.DeSpawn(mode);
    }

    protected override void Tick()
    {
        base.Tick();
        if (!this.IsHashIntervalTick(60) || paused) return;
        if (billGiver != null)
        {
            if (currentBill != null)
            {
                PollBill();
            }
            else if (billGiver.Destroyed)
            {
                billGiver = null;
            }
            else if (billGiver.BillStack.Count > 0)
            {
                foreach (var bill in billGiver.BillStack)
                {
                    if (!bill.ShouldDoNow()) continue;
                    TryStartBill(billGiver.BillStack.FirstShouldDoNow);
                    if (currentBill != null)
                    {
                        break;
                    }
                }
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
            if (dynWorkRate != null)
            {
                sb.AppendLine();
                if (paused)
                {
                    sb.AppendFormat("<color=yellow>{0}</color>", "Paused".Translate());
                }
                else
                {
                    sb.AppendLine(WorkingStatusStr);
                    sb.AppendFormat("{0}: ", "WorkSpeed".Translate());
                    if (overclocking == 0)
                    {
                        sb.AppendFormat("<color={1}>{0}</color> op/s", dynWorkRate.Value,
                            dynWorkRate.Low ? "yellow" : "green");
                    }
                    else
                    {
                        sb.AppendFormat(
                            "Nex_WorkerOverclocked".Translate(dynWorkRate.Value, WorkSpeedByOc(overclocking), overclocking)
                            //"Overclocked to <color=red>{0}</color> ops, speed: <color=green>{1}</color> op/s. {2}GHz",
                        );
                    }


                    if (dynWorkRate.Low)
                    {
                        sb.AppendFormat(" ({0})", "Nex_LowPower".Translate());
                    }
                }
            }
            else
            {
                sb.AppendInNewLine("Nex_NoIdleWorkrate".Translate());
            }

            sb.AppendInNewLine($"{"Nex_WorkLeft".Translate()}: {workLeft} / {workTotal} (x{speedFactor:P2})");
            sb.AppendInNewLine($"{"Nex_Ingredients".Translate()}: {ingredients.Select(t => t.LabelCap).Join()}");
        }
        else
        {
            if (billGiver == null)
            {
                sb.AppendLine("Nex_NotLinkedWorkbench".Translate());
            }
            else
            {
                sb.AppendLine("Nex_WaitForWork".Translate());
            }
        }

        return sb.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Settle()
        {
            defaultLabel = "Nex_SetPriority".Translate(),
            defaultDesc = "Nex_SetPriority.desc".Translate(),
            icon = Assets.Priority,
            action = () =>
            {
                Find.WindowStack.Add(new Dialog_SliderWithInput(
                    val => $"Storage Priority: {val}",
                    -50,
                    100,
                    value =>
                    {
                        priority = value;
                        if (dynWorkRate != null)
                        {
                            dynWorkRate = Network.ChangeProperty(dynWorkRate, value);
                        }
                    },
                    priority
                ));
            }
        };
        yield return new Command_Settle()
        {
            defaultLabel = "Nex_Overclocking".Translate(),
            defaultDesc = "Nex_Overclocking.desc".Translate(),
            icon = Assets.Overclocking,
            action = () =>
            {
                Find.WindowStack.Add(new Dialog_SliderWithInput(
                    val => $"Max Overclocking Level: {val}",
                    0,
                    25,
                    value => { overclockingLimit = value; },
                    overclockingLimit
                ));
            }
        };
        if (currentBill != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "Cancel".Translate(),
                icon = TexButton.CloseXBig,
                action = () => DoneBill(true),
            };
        }

        if (paused)
        {
            yield return new Command_Action
            {
                defaultLabel = "Resume".Translate(),
                icon = Assets.Resume,
                action = () => paused = false
            };
        }
        else
        {
            yield return new Command_Action
            {
                defaultLabel = "Pause".Translate(),
                icon = Assets.Pause,
                action = () =>
                {
                    paused = true;
                    if (dynWorkRate != null)
                    {
                        Network.ReleaseWorkRate(dynWorkRate);
                    }
                }
            };
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref currentBill, "currentBill");
        Scribe_Deep.Look(ref unfinishedThing, "unfinishedThing");
        Scribe_References.Look(ref dominant, "dominant");
        Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Reference);
        Scribe_Values.Look(ref workTotal, "workTotal");
        Scribe_Values.Look(ref workLeft, "workLeft");
        Scribe_Values.Look(ref speedFactor, "speedFactor");
        Scribe_Values.Look(ref WorkingStatusStr, "WorkingStatusStr", "");
        Scribe_Values.Look(ref paused, "paused");
        Scribe_Values.Look(ref overclockingLimit, "overclockingLimit");
        Scribe_Values.Look(ref priority, "priority");
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
            var reservation1 =
                new ReservationManager.Reservation(StaticSaveData.Worker, StaticSaveData.workJob, 1, -1, table, null);
            Traverse.Create(Map.reservationManager).Field("reservations")
                .GetValue<List<ReservationManager.Reservation>>().Add(reservation1);
            Map.events.Notify_ReservationAdded(reservation1);
        }
    }

    private float CalcWorkSpeed()
    {
        var ops = Math.Min(WorkSpeedByOc(overclocking), dynWorkRate!.Value) * speedFactor;
        return ops;
    }

    private float WorkrateByOc(int level) => BASE_TARGET_WORKRATE * Mathf.Pow(2, level);
    private float WorkSpeedByOc(int level) => BASE_TARGET_WORKRATE * Mathf.Pow(1.5f, level);

    // 高优先级试图超频时不会使得已有的低优先级worker降频
    // 可以做到但是为了复杂度和性能考虑，还是不要一遍又一遍的遍历DynWorkRates了
    private void TryOverclocking()
    {
        var requireWorkrate = Mathf.RoundToInt(WorkrateByOc(overclocking + 1)) - dynWorkRate?.Value ?? 0;
        if (dynWorkRate == null || dynWorkRate.Low || overclocking >= overclockingLimit ||
            Network.AvailableWorkrate < requireWorkrate)
        {
            TryUnderclocking();
            return;
        }

        var newWorkRate = Network.RequestWorkrate(requireWorkrate, priority, skipDeviceCheck: true);
        if (newWorkRate == null)
        {
            return;
        }

        Network.ReleaseWorkRate(newWorkRate, true);
        dynWorkRate.Value += newWorkRate.Value;
        dynWorkRate.Expected += newWorkRate.Expected;
        overclocking += 1;
    }

    private void TryUnderclocking()
    {
        while (dynWorkRate != null && dynWorkRate.Low && overclocking > 0)
        {
            var requireWorkrate = Mathf.RoundToInt(WorkrateByOc(overclocking - 1));
            Network.ReleaseWorkRate(dynWorkRate);
            dynWorkRate = Network.RequestWorkrate(requireWorkrate, priority);
            if (dynWorkRate == null)
            {
                overclocking = 0;
            }
            else
            {
                overclocking -= 1;
            }
        }
    }

    private void TryRequestWorkrate()
    {
        if (dynWorkRate == null)
        {
            dynWorkRate = Network.RequestWorkrate(BASE_TARGET_WORKRATE, priority);
            overclocking = 0;
            return;
        }

        if (dynWorkRate.Low && this.IsHashIntervalTick(120))
        {
            Network.ReleaseWorkRate(dynWorkRate);
            dynWorkRate = Network.RequestWorkrate(dynWorkRate.Expected, priority);
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
            unfThing.Creator = StaticSaveData.Worker;
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

        speedFactor = billGiver.GetStatValue(bill.recipe.workTableSpeedStat);
        workTotal = workLeft;
        currentBill = billProduction;
        ingredients = ingres;
        bill.SetPawnRestriction(StaticSaveData.Worker);
        bill.Notify_BillWorkStarted(null);

        WorkingStatusStr = JobUtility.GetResolvedJobReport(currentBill.recipe.jobString, billGiver,
            ingredients.Count > 0 ? ingredients[0] : LocalTargetInfo.Invalid);
        TryRequestWorkrate();
    }

    private void PollBill()
    {
        if (currentBill == null)
        {
            return;
        }

        if (dynWorkRate?.Released ?? false)
        {
            dynWorkRate = null;
        }

        if (dynWorkRate == null || dynWorkRate.Low)
        {
            TryRequestWorkrate();
            if (dynWorkRate == null)
            {
                return;
            }
        }

        TryOverclocking();
        workLeft -= CalcWorkSpeed();
        if (workLeft <= 0)
        {
            DoneBill();
            return;
        }

        if (progressBarEffecter == null)
        {
            progressBarEffecter = EffecterDefOf.ProgressBarAlwaysVisible.SpawnAttached(this, Map);
        }

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
            var products = utils.Recipe.MakeRecipeProducts(currentBill.recipe, StaticSaveData.Worker, ingredients,
                    dominant, billGiver!, currentBill.precept, style, CalcQuality(), currentBill.graphicIndexOverride)
                .ToList();

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
        overclocking = 0;
        ingredients.Clear();

        if (dynWorkRate != null)
        {
            Network.ReleaseWorkRate(dynWorkRate);
            dynWorkRate = null;
        }
    }

    private QualityCategory CalcQuality()
    {
        if (overclocking <= 2)
        {
            return QualityUtility.GenerateFromGaussian(2f, QualityCategory.Good, QualityCategory.Normal,
                QualityCategory.Poor);
        }
        else if (overclocking <= 5)
        {
            return QualityUtility.GenerateFromGaussian(1.5f, QualityCategory.Excellent, QualityCategory.Good,
                QualityCategory.Normal);
        }
        else if (overclocking <= 7)
        {
            return QualityUtility.GenerateFromGaussian(1.5f, QualityCategory.Masterwork, QualityCategory.Excellent,
                QualityCategory.Good);
        }
        else if (overclocking > 8)
        {
            return QualityUtility.GenerateFromGaussian(1.5f, QualityCategory.Legendary, QualityCategory.Excellent,
                QualityCategory.Excellent);
        }
        else
        {
            return QualityUtility.GenerateFromGaussian(1.5f, QualityCategory.Legendary, QualityCategory.Masterwork,
                QualityCategory.Excellent);
        }
    }
}

// | 超频等级 | 所需工作率 | 工作速度 |
// |---------|--------|----------|
// | 0 | 60 | 60.0 |
// | 1 | 120 | 90.0 |
// | 2 | 240 | 135.0 |
// | 3 | 480 | 202.5 |
// | 4 | 960 | 303.75 |
// | 5 | 1,920 | 455.63 |
// | 6 | 3,840 | 683.44 |
// | 7 | 7,680 | 1,025.16 |
// | 8 | 15,360 | 1,537.74 |
// | 9 | 30,720 | 2,306.61 |
// | 10 | 61,440 | 3,459.91 |
// | 11 | 122,880 | 5,189.87 |
// | 12 | 245,760 | 7,784.80 |
// | 13 | 491,520 | 11,677.20 |
// | 14 | 983,040 | 17,515.80 |
// | 15 | 1,966,080 | 26,273.71 |
// | 16 | 3,932,160 | 39,410.56 |
// | 17 | 7,864,320 | 59,115.84 |
// | 18 | 15,728,640 | 88,673.76 |
// | 19 | 31,457,280 | 133,010.64 |
// | 20 | 62,914,560 | 199,515.96 |
// | 21 | 125,829,120 | 299,273.95 |
// | 22 | 251,658,240 | 448,910.92 |
// | 23 | 503,316,480 | 673,366.38 |
// | 24 | 1,006,632,960 | 1,010,049.57 |
// | 25 | 2,013,265,920 | 1,515,074.35 |