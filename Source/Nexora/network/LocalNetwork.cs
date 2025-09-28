using Nexora.buildings;
using Nexora.Patches;
using Nexora.utils.pooled;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Nexora.network;

public class LocalNetwork(Map map) : MapComponent(map), IItemInterface
{
    public event Action? OnItemChanged;

    public readonly HashSet<IItemInterface> Storages = [];
    public readonly List<IItemInterface> SortedStorages = [];

    public readonly HashSet<Building_AccessInterface> AccessInterfaces = [];

    public readonly HashSet<Building_CPU> Cpus = [];
    public List<DynWorkRate> DynWorkRates = [];

    public int AvailableWorkrate => TotalWorkrate - UsedWorkrate;
    public int UsedWorkrate { get; set; }
    public int TotalWorkrate { get; set; }
    public int MaxDevices { get; set; }

    public LocalNetwork Network() => this;

    public void Connect(Building_CPU cpu)
    {
        Cpus.Add(cpu);
        OnCpuChange();
    }

    public bool Disconnect(Building_CPU cpu)
    {
        var success = Cpus.Remove(cpu);
        OnCpuChange();
        return success;
    }

    public DynWorkRate? RequestWorkrate(int value, int priority, int? expected = null, bool skipDeviceCheck = false)
    {
        for (var i = DynWorkRates.Count - 1; i >= 0 && AvailableWorkrate < value; i--)
        {
            var lowerPriorityWorkRate = DynWorkRates[i];
            if (lowerPriorityWorkRate.Priority >= priority)
            {
                break;
            }

            if (!skipDeviceCheck && DynWorkRates.Count >= MaxDevices)
            {
                ReleaseWorkRate(lowerPriorityWorkRate);
                continue;
            }

            if (AvailableWorkrate < value)
            {
                if (lowerPriorityWorkRate.Value <= value)
                {
                    ReleaseWorkRate(lowerPriorityWorkRate);
                    continue;
                }

                ReleaseWorkRate(lowerPriorityWorkRate, value);
            }
        }

        var num = Math.Min(AvailableWorkrate, value);
        if (num > 0)
        {
            UsedWorkrate += num;
            var dynWorkRate = new DynWorkRate(num, priority)
            {
                Expected = expected ?? value,
            };
            var index = DynWorkRates.BinarySearch(dynWorkRate,
                Comparer<DynWorkRate>.Create((a, b) => b.Priority.CompareTo(a.Priority)));
            DynWorkRates.Insert(index < 0 ? ~index : index, dynWorkRate);
            return dynWorkRate;
        }

        return null;
    }

    public void ReleaseWorkRate(DynWorkRate workRate, int value)
    {
        if (workRate.Released)
        {
            Log.Error("[Nexora] duplicate release of workrate");
            return;
        }

        if (value >= workRate.Value)
        {
            ReleaseWorkRate(workRate);
            return;
        }

        workRate.Value -= value;
        UsedWorkrate -= value;
    }

    public void ReleaseWorkRate(DynWorkRate workRate, bool keepValue = false)
    {
        if (workRate.Released)
        {
            Log.Error("[Nexora] duplicate release of workrate");
            return;
        }

        if (!keepValue)
        {
            UsedWorkrate -= workRate.Value;
        }

        DynWorkRates.Remove(workRate);
        workRate.Released = true;
    }

    public void Connect(IItemInterface storage)
    {
        if (!Storages.Add(storage) || storage is LocalNetwork)
        {
            return;
        }

        var index = SortedStorages.BinarySearch(storage,
            Comparer<IItemInterface>.Create((a, b) => b.Priority().CompareTo(a.Priority())));
        SortedStorages.Insert(index < 0 ? ~index : index, storage);
    }

    public bool Disconnect(IItemInterface storage)
    {
        return Storages.Remove(storage) && SortedStorages.Remove(storage);
    }

    public bool ContainsStorage(ItemStorage storage)
    {
        var b1 = Storages.Contains(storage);
        var b2 = SortedStorages.Contains(storage);
        if (b1 != b2)
        {
            Log.Error("[Nexora] storage in Storages but not in SortedStorages (or vice versa)");
        }

        return b1;
    }

    public DynWorkRate? ChangeProperty(DynWorkRate workRate, int priority)
    {
        if (!workRate.Released)
        {
            workRate.Priority = priority;
            ReleaseWorkRate(workRate);
            return RequestWorkrate(workRate.Expected, priority);
        }

        return null;
    }

    public void ChangeProperty(ItemStorage storage, int priority)
    {
        if (Disconnect(storage))
        {
            storage.priority = priority;
            Connect(storage);
        }
    }

    public void ChangeProperty(Building_ExternalStorageConnector storage, int priority)
    {
        if (Disconnect(storage))
        {
            storage.priority = priority;
            Connect(storage);
        }
    }

    public IEnumerable<Thing> GetItemsByDef(ThingDef def)
    {
        return SortedStorages.SelectMany(s => s.GetItemsByDef(def));
    }

    public int TryAddItem(Thing item)
    {
        var total = item.stackCount;
        var added = 0;
        foreach (var storage in SortedStorages)
        {
            added += storage.TryAddItem(item);
            if (added >= total)
            {
                break;
            }
        }

        if (added > 0)
        {
            OnItemChanged?.Invoke();
        }

        return added;
    }

    public void AddItemOrOverflow(Thing item, IntVec3 pos)
    {
        var total = item.stackCount;
        var added = TryAddItem(item);
        if (added != total && item.stackCount > 0 && !item.Destroyed)
        {
            GenDrop.TryDropSpawn(item, pos, map, ThingPlaceMode.Near, out _);
        }
    }

    public IEnumerable<Thing> GetVirtualItems()
    {
        return Storages.SelectMany(s => s.GetVirtualItems());
    }

    public IEnumerable<Thing> GetExternalItems()
    {
        return Storages.SelectMany(s => s.GetExternalItems());
    }

    public IEnumerable<Thing> GetAllItems()
    {
        return Storages.SelectMany(s => s.GetAllItems());
    }

    private void OnCpuChange()
    {
        TotalWorkrate = (int)Cpus.Sum(cpu => cpu.TotalWorkrate);
        MaxDevices = (int)Cpus.Sum(cpu => cpu.WorkrateComp.Props.maxDevices);
        if (DynWorkRates.Count > MaxDevices)
        {
            for (var i = DynWorkRates.Count - 1; i >= MaxDevices; i--)
            {
                ReleaseWorkRate(DynWorkRates[i]);
            }
        }

        if (UsedWorkrate > TotalWorkrate)
        {
            var lack = UsedWorkrate - TotalWorkrate;
            for (var i = DynWorkRates.Count - 1; i >= 0; i--)
            {
                var dynWorkRate = DynWorkRates[i];
                if (dynWorkRate.Value <= lack)
                {
                    lack -= dynWorkRate.Value;
                    ReleaseWorkRate(dynWorkRate);
                }
                else
                {
                    dynWorkRate.Value -= lack;
                    UsedWorkrate -= lack;
                    break;
                }
            }

            if (UsedWorkrate != TotalWorkrate)
            {
                Log.Warning(
                    $"[Nexora] Reduce Workrate exception, UsedWorkrate: {UsedWorkrate}, TotalWorkrate: {TotalWorkrate}");
                UsedWorkrate = TotalWorkrate;
            }
        }

        for (var i = 0; i < DynWorkRates.Count; i++)
        {
            if (AvailableWorkrate <= 0)
            {
                break;
            }

            var dynWorkRate = DynWorkRates[i];
            if (!dynWorkRate.Low) continue;

            var lack = dynWorkRate.Expected - dynWorkRate.Value;
            if (lack < 0)
            {
                ReleaseWorkRate(dynWorkRate, -lack);
            }
            else
            {
                var num = Math.Min(lack, AvailableWorkrate);
                dynWorkRate.Value += num;
                UsedWorkrate += num;
            }
        }
    }

    public NetworkInfo Info() => new()
    {
        OwnerMap = map.Parent.LabelCap ?? "unknown map",
        StorageCount = Storages.Count
    };

    public int GetCountCanAccept(Thing item)
    {
        long sum = 0;
        foreach (var storage in SortedStorages)
        {
            sum += storage.GetCountCanAccept(item);
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)sum;
    }

    public int Count()
    {
        long sum = 0;
        foreach (var storage in Storages)
        {
            sum += storage.Count();
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)sum;
    }

    public int Priority() => 0;

    public bool Contains(Thing item)
    {
        return SortedStorages.Any(storage => storage.Contains(item));
    }

    public bool Managed(Thing item)
    {
        return item.holdingOwner is ItemStorage or EmptyThingOwner ||
               Storages.Where(s => s is Building_ExternalStorageConnector).Any(s => s.Contains(item));
    }

    public IEnumerable<Thing> GetItemByRequest(ThingRequest request)
    {
        return SortedStorages.SelectMany(storage =>
        {
            switch (storage)
            {
                case ItemStorage itemStorage:
                    if (request.singleDef != null)
                    {
                        return itemStorage.IndexTable.TryGetValue(request.singleDef, out var things)
                            ? things.Keys.AsEnumerable()
                            : [];
                    }
                    else
                    {
                        return itemStorage.IndexTable.Where(pair =>
                            request.group.Includes(pair.Key)
                        ).SelectMany(pair => pair.Value.Keys.AsEnumerable());
                    }
                case Building_ExternalStorageConnector connector:
                    return connector.GetExternalItems().Where(t => request.Accepts(t));
            }

            Log.Error($"[Nexora] GetItemByRequest: unexpect storage: {storage.GetType().FullName}");
            return [];
        });
    }

    public Building_AccessInterface? GetClosestAccessInterface(IntVec3 position, float? searchRadius = null,
        PathEndMode pathEndMode = PathEndMode.ClosestTouch, TraverseParms? traverseParams = null)
    {
        Building_AccessInterface? closest = null;
        var searchRadiusSquared = !searchRadius.HasValue ? float.MaxValue : Mathf.Sqrt(searchRadius.Value);

        foreach (var @interface in AccessInterfaces)
        {
            if (!@interface.IsForbidden(Faction.OfPlayer) && @interface.OutputEnabled &&
                map.reachability.CanReach(position, @interface, pathEndMode,
                    traverseParams ?? TraverseParms.For(TraverseMode.ByPawn)))
            {
                var distSq = position.DistanceToSquared(@interface.Position);
                if (distSq <= searchRadiusSquared)
                {
                    searchRadiusSquared = distSq;
                    closest = @interface;
                }
            }
        }

        return closest;
    }

    public IEnumerable<Building_AccessInterface> GetAccessInterfaces(IntVec3? canReach = null,
        float radius = float.MaxValue)
    {
        return map.listerBuildings.AllBuildingsColonistOfClass<Building_AccessInterface>()
            .Where(i =>
            {
                if (!canReach.HasValue)
                {
                    return true;
                }

                return canReach.Value.DistanceTo(i.Position) <= radius && map.reachability.CanReach(
                    canReach.Value, i, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.ByPawn));
            });
    }

    public void AutoOrganize()
    {
        using var storages = SortedStorages.ToPooledList();
        for (var i = storages.Count - 1; i >= 1; i--)
        {
            var storage = storages[i];
            foreach (var item in storage.GetAllItems().ToList())
            {
                if (storages[i - 1].GetCountCanAccept(item) > 0)
                {
                    TryAddItem(item.SplitOff(item.stackCount));
                }
            }
        }
    }
}