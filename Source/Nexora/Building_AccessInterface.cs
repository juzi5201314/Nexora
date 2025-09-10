using Nexora.network;
using Nexora.ui;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nexora;

[StaticConstructorOnStartup]
public class Building_AccessInterface : Building, IHaulDestination, IThingHolder, IRenameable
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    private StorageSettings settings = new();
    private bool haulDestinationEnabled = true;
    private bool outputEnabled = true;
    private string customName = "";

    internal readonly AccessInterfaceThingOwnerProxy InnerThingOwner;

    public string RenamableLabel
    {
        get => customName;
        set => customName = value;
    }

    public string BaseLabel => Label;
    public string InspectLabel => RenamableLabel;

    public bool StorageTabVisible => true;
    public bool HaulDestinationEnabled => haulDestinationEnabled;
    public bool OutputEnabled => outputEnabled;

    public Building_AccessInterface()
    {
        InnerThingOwner = new AccessInterfaceThingOwnerProxy(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        Network.AccessInterfaces.Add(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        Network.AccessInterfaces.Remove(this);
    }

    public override void PostMake()
    {
        base.PostMake();
        settings = new StorageSettings(null)
        {
            filter = new ThingFilter(),
            Priority = StoragePriority.Normal,
        };
        settings.filter.SetAllowAll(null);
    }

    public StorageSettings GetStoreSettings() => settings;

    public StorageSettings GetParentStoreSettings() =>
        def.building.fixedStorageSettings ?? StorageSettings.EverStorableFixedSettings();

    public void Notify_SettingsChanged()
    {
    }

    public bool Accepts(Thing t)
    {
        return settings.AllowedToAccept(t) && Network.Storages.Select(storage => storage.GetCountCanAccept(t))
            .Any(accepts => accepts > 0);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref settings, "settings", this);
        Scribe_Values.Look(ref customName, "customName", LabelCap);
        Scribe_Values.Look(ref haulDestinationEnabled, "haulDestinationEnabled", true);
        Scribe_Values.Look(ref outputEnabled, "outputEnabled", true);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Toggle()
        {
            defaultLabel = "Enable Input",
            icon = Assets.Input,
            toggleAction = () => haulDestinationEnabled = !haulDestinationEnabled,
            isActive = () => haulDestinationEnabled,
        };
        yield return new Command_Toggle()
        {
            defaultLabel = "Enable Output",
            icon = Assets.Output,
            toggleAction = () => outputEnabled = !outputEnabled,
            isActive = () => outputEnabled,
        };
        yield return new Command_Action
        {
            defaultLabel = "Rename".Translate(),
            icon = TexButton.Rename,
            action = () => Find.WindowStack.Add(new DialogRename(this)),
        };
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return InnerThingOwner;
    }

    public override void TickRare()
    {
        base.TickRare();
    }
}

public class AccessInterfaceThingOwnerProxy(Building_AccessInterface parent) : ThingOwner(parent)
{
    // 此处的Thing应当只是暂时存放在访问接口中
    // 它们应该马上会被拿走
    // 或者在不需要使用时马上被返回到存储网络中
    public readonly Dictionary<Thing, object> Temp = [];

    public void AddTempThing(Thing thing)
    {
        thing.holdingOwner = this;
        Temp.Add(thing, typeof(void));
    }

    public bool ReturnToNetwork(Thing thing)
    {
        if (Remove(thing))
        {
            parent.Network.TryAddItem(thing);
            if (thing.stackCount != 0 || !thing.Destroyed)
            {
                GenDrop.TryDropSpawn(thing, parent.Position, parent.Map, ThingPlaceMode.Near, out _);
                return false;
            }

            return true;
        }

        Log.Warning($"[Nexora] Failed to return {thing.LabelCap} to network.");
        return false;
    }

    public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
    {
        throw new NotImplementedException();
    }

    public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
    {
        return parent.Network.TryAddItem(item) > 0;
    }

    public override int IndexOf(Thing? item)
    {
        throw new NotImplementedException();
    }

    public override bool Remove(Thing? item)
    {
        if (item is null || item.holdingOwner != this || !Temp.Remove(item)) return false;
        item.holdingOwner = null;
        return true;
    }

    public override int Count => 0;

    protected override Thing GetAt(int index)
    {
        throw new NotImplementedException();
    }

    public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
    {
        return parent.Network.GetCountCanAccept(item);
    }

    public new bool Contains(Thing item)
    {
        return parent.Network.Contains(item);
    }
}

public class BillTargetProxy : ISlotGroupParent
{
    public readonly Building_AccessInterface Interface;
    private readonly SlotGroup dummySlotGroup;

    public bool StorageTabVisible => false;
    public IntVec3 Position => Interface.Position;
    public Map Map => Interface.Map;
    public bool HaulDestinationEnabled => false;

    public string SlotYielderLabel() => $"{Interface.RenamableLabel}";
    public SlotGroup GetSlotGroup() => dummySlotGroup;
    public bool IgnoreStoredThingsBeauty => true;
    public string GroupingLabel => Interface.LabelCap;
    public int GroupingOrder => 0;

    public StorageSettings GetStoreSettings() => Interface.GetStoreSettings();
    public void Notify_SettingsChanged() => Interface.Notify_SettingsChanged();
    public StorageSettings GetParentStoreSettings() => Interface.GetParentStoreSettings();

    public BillTargetProxy(Building_AccessInterface building)
    {
        Interface = building;
        dummySlotGroup = new SlotGroup(this);
    }

    public IEnumerable<IntVec3> AllSlotCells()
    {
        yield break;
    }

    public List<IntVec3> AllSlotCellsList()
    {
        return [];
    }

    public void Notify_ReceivedThing(Thing newItem)
    {
    }

    public void Notify_LostThing(Thing newItem)
    {
    }

    public bool Accepts(Thing t)
    {
        return false;
    }
}