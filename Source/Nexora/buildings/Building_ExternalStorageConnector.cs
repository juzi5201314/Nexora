using Nexora.comp;
using Nexora.network;
using Nexora.ui;
using Nexora.ui.utils;
using Nexora.utils.pooled;
using RimWorld;
using Verse;

namespace Nexora.buildings;

public class Building_ExternalStorageConnector : Building, IItemInterface
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    public HashSet<IntVec3> CellsInRange = [];
    internal int priority = 0;

    public readonly HashSet<Building_Storage> ExternalStorages = [];
    public readonly HashSet<IThingHolder> ExternalContainers = [];

    public Building_ExternalStorageConnector()
    {
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Network.Connect(this);
        CellsInRange = GetComp<CompRanged>().CellInRange().ToHashSet();
        Map.events.BuildingSpawned += OnBuildingSpawned;
        Map.events.BuildingDespawned += OnBuildingDespawned;
        foreach (var building in CellsInRange.Select(vec3 => Map.GetFirstBuilding<Building>(vec3)).OfType<Building>())
        {
            OnBuildingSpawned(building);
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.events.BuildingSpawned -= OnBuildingSpawned;
        Map.events.BuildingDespawned -= OnBuildingDespawned;
        foreach (var building in CellsInRange.Select(vec3 => Map.GetFirstBuilding<Building>(vec3)).OfType<Building>())
        {
            OnBuildingDespawned(building);
        }

        Network.Disconnect(this);

        base.DeSpawn(mode);
    }

    private void OnBuildingSpawned(Building building)
    {
        if (IsUnAllowedBuilding(building) || !CellsInRange.Contains(building.Position))
        {
            return;
        }

        switch (building)
        {
            case IThingHolder holder:
                ExternalContainers.Add(holder);
                break;
            case Building_Storage storage:
                foreach (var thing in storage.slotGroup.HeldThings)
                {
                    thing.holdingOwner = EmptyThingOwner.Instance;
                }

                ExternalStorages.Add(storage);
                break;
        }
    }

    private void OnBuildingDespawned(Building building)
    {
        if (IsUnAllowedBuilding(building) || !CellsInRange.Contains(building.Position))
        {
            return;
        }

        switch (building)
        {
            case IThingHolder holder:
                ExternalContainers.Remove(holder);
                break;
            case Building_Storage storage:
                ExternalStorages.Remove(storage);
                break;
        }
    }

    private static bool IsUnAllowedBuilding(Building building) =>
        building is Building_LocalStorage or Building_ExternalStorageConnector or Building_AccessInterface;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref priority, "priority");
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        foreach (var building in ExternalStorages)
        {
            GenDraw.DrawTargetHighlightWithLayer(building.Position, AltitudeLayer.MetaOverlays);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        yield return new Command_Action
        {
            defaultLabel = "Open Terminal".Translate(),
            icon = Assets.Terminal,
            hotKey = DefDatabase<KeyBindingDef>.GetNamed("Nexora_HotKey_N"),
            action = () => { Find.WindowStack.Add(new Window_Terminal(this)); }
        };
        yield return new Command_Action
        {
            defaultLabel = "Transfer Items".Translate(),
            defaultDesc = "Transfer items to other storage drives",
            icon = Assets.MoveItem,
            action = () =>
            {
                if (!Network.Disconnect(this))
                {
                    Log.Error("[Nexora] Can't disconnect storage drive");
                    return;
                }

                foreach (var thing in GetExternalItems().ToPooledList())
                {
                    var num = Math.Min(Network.GetCountCanAccept(thing), thing.stackCount);
                    Network.TryAddItem(thing.SplitOff(num));
                }

                Network.Connect(this);
            }
        };
        yield return new Command_Settle()
        {
            defaultLabel = "Set Priority".Translate(),
            icon = Assets.Priority,
            action = () =>
            {
                Find.WindowStack.Add(new Dialog_SliderWithInput(
                    val => $"Storage Priority: {val}",
                    -50,
                    100,
                    value => { Network.ChangeProperty(this, value); },
                    priority
                ));
            }
        };
    }

    public IEnumerable<Thing> GetVirtualItems()
    {
        yield break;
    }

    public IEnumerable<Thing> GetExternalItems()
    {
        return ExternalStorages.SelectMany(storage => storage.slotGroup.HeldThings)
            .Concat(ExternalContainers.SelectMany(container => container.GetDirectlyHeldThings()));
    }

    public IEnumerable<Thing> GetAllItems() => GetExternalItems();

    public int TryAddItem(Thing item)
    {
        var added = 0;
        var total = item.stackCount;
        foreach (var storage in ExternalStorages)
        {
            foreach (var cell in storage.AllSlotCells())
            {
                if (cell.IsValidStorageFor(Map, item))
                {
                    if (GenDrop.TryDropSpawn(item, cell, Map, ThingPlaceMode.Direct, out var res))
                    {
                        if (res != null)
                        {
                            added += res.stackCount;
                            res.holdingOwner = EmptyThingOwner.Instance;
                        }

                        if (added >= total || item.Destroyed || item.stackCount <= 0)
                        {
                            return added;
                        }
                    }
                }
            }
        }

        foreach (var container in ExternalContainers)
        {
            var thingOwner = container.GetDirectlyHeldThings();
            if (thingOwner.GetCountCanAccept(item) >= 0)
            {
                added += thingOwner.TryAddOrTransfer(item, item.stackCount);
                if (item.Destroyed || item.stackCount <= 0)
                {
                    return added;
                }
            }
        }

        return added;
    }

    public int GetCountCanAccept(Thing item)
    {
        return ExternalStorages.Sum(s => s.Accepts(item) ? 1 : 0) +
               ExternalContainers.Sum(h => h.GetDirectlyHeldThings().GetCountCanAccept(item));
    }


    public int Count()
    {
        return ExternalStorages.Sum(s => s.slotGroup.HeldThingsCount) +
               ExternalContainers.Sum(h => h.GetDirectlyHeldThings().Count);
    }

    public bool Contains(Thing item)
    {
        return item.holdingOwner is EmptyThingOwner
               || ExternalContainers.Any(h => h.GetDirectlyHeldThings().Contains(item));
    }

    LocalNetwork IItemInterface.Network() => Network;
    public int Priority() => priority;
}

// 一个空的ThingOwner，仅用于判断放置在地图格子上的Thing是否包含在ExternalStorage中
public class EmptyThingOwner : ThingOwner
{
    public static readonly EmptyThingOwner Instance = new();

    public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
    {
        throw new NotImplementedException();
    }

    public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
    {
        throw new NotImplementedException();
    }

    public override int IndexOf(Thing? item)
    {
        throw new NotImplementedException();
    }

    public override bool Remove(Thing? item)
    {
        if (item?.holdingOwner != null)
        {
            item.holdingOwner = null;
        }

        return true;
    }

    public override int Count => 0;

    protected override Thing GetAt(int index)
    {
        throw new NotImplementedException();
    }
}