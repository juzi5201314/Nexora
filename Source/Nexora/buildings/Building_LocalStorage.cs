using System.Text;
using Nexora.comp;
using Nexora.network;
using Nexora.ui;
using Nexora.utils.pooled;
using RimWorld;
using Verse;

namespace Nexora.buildings;

public class Building_LocalStorage : Building, IThingHolder, IHaulSource
{
    public ItemStorage? Storage;
    public CompDataFormat CompDataFormat = new CompDataFormatUnlimited();

    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        CompDataFormat = GetComp<CompDataFormat>() ?? new CompDataFormatUnlimited();
        var power = GetComp<CompPowerTrader>();
        if (power != null)
        {
            if (power.PowerOn)
            {
                Network.Connect(Storage!);
            }

            power.powerStartedAction = () => Network.Connect(Storage!);
            power.powerStoppedAction = () => Network.Disconnect(Storage!);
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Network.Disconnect(Storage!);
        base.DeSpawn(mode);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode is DestroyMode.Deconstruct or DestroyMode.KillFinalizeLeavingsOnly)
        {
            // emmmm
        }

        base.Destroy(mode);
    }

    public override void PostMake()
    {
        base.PostMake();
        Storage ??= new ItemStorage(this);
    }

    public override void PreSwapMap()
    {
        base.PreSwapMap();
        Network.Disconnect(Storage!);
    }

    public override void PostSwapMap()
    {
        base.PostSwapMap();
        Network.Connect(Storage!);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref Storage, "storage", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Storage ??= new ItemStorage(this);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield return new Command_Action
        {
            defaultLabel = "Open Terminal".Translate(),
            icon = Assets.Terminal,
            hotKey = DefDatabase<KeyBindingDef>.GetNamed("Nexora_HotKey_N"),
            action = () => { Find.WindowStack.Add(new Window_Terminal(Storage!)); }
        };
        yield return new Command_Action
        {
            defaultLabel = "Transfer Items".Translate(),
            defaultDesc = "Transfer items to other storage drives",
            icon = Assets.MoveItem,
            action = () =>
            {
                if (!Network.Disconnect(Storage!) || Network.ContainsStorage(Storage!))
                {
                    Log.Error("[Nexora] Can't disconnect storage drive");
                    return;
                }

                using var pooledList = Storage!.ToPooledList();
                foreach (var thing in pooledList)
                {
                    var canAccept = Network.GetCountCanAccept(thing);
                    if (canAccept > 0)
                    {
                        var num = Math.Min(canAccept, thing.stackCount);
                        Network.TryAddItem(thing.SplitOff(num));
                    }
                }

                Network.Connect(Storage!);
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
                    value => { Network.ChangeProperty(Storage!, value); },
                    Storage?.priority ?? 0
                ));
            }
        };

        if (Prefs.DevMode)
        {
            yield return new Command_Action()
            {
                defaultLabel = "RandomAddItemTest".Translate(),
                action = () => { DebugActions.RandomAddItemToStorage(Storage!); }
            };
        }

        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
    }

    public new IThingHolder ParentHolder => Map;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        //ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return Storage!;
    }

    public override string GetInspectString()
    {
        var s = new StringBuilder(base.GetInspectString());
        s.AppendInNewLine($"{"Priority".Translate()}: {Storage!.Priority()}");
        foreach (var s1 in CompDataFormat.GetExtraInspectString(Storage!))
        {
            s.AppendInNewLine(s1);
        }

        return s.ToString();
    }

    // source

    public StorageSettings GetStoreSettings()
    {
        return StorageSettings.EverStorableFixedSettings();
    }

    public StorageSettings GetParentStoreSettings()
    {
        return StorageSettings.EverStorableFixedSettings();
    }

    public void Notify_SettingsChanged()
    {
    }

    public bool StorageTabVisible => true;
    public bool HaulSourceEnabled => false;
}