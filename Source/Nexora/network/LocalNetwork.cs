using Nexora.buildings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Nexora.network;

public class LocalNetwork(Map map) : MapComponent(map), IItemInterface
{
    public readonly HashSet<IItemInterface> Storages = [];
    public readonly List<IItemInterface> SortedStorages = [];

    public readonly HashSet<Building_AccessInterface> AccessInterfaces = [];

    public LocalNetwork Network() => this;

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

        return added;
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
}