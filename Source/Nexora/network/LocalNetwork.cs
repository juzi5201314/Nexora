using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.network;

public class LocalNetwork(Map map) : MapComponent(map), IItemInterface
{
    public readonly HashSet<ItemStorage> Storages = [];
    public readonly List<ItemStorage> SortedStorages = [];

    public readonly HashSet<Building_AccessInterface> AccessInterfaces = [];

    public LocalNetwork Network() => this;

    public void Connect(ItemStorage storage)
    {
        if (!Storages.Add(storage))
        {
            return;
        }

        for (var i = 0; i < SortedStorages.Count; i++)
        {
            var s = SortedStorages[i];
            if (s.Priority <= storage.Priority)
            {
                SortedStorages.Insert(i, storage);
                return;
            }
        }

        SortedStorages.Add(storage);
    }

    public bool Disconnect(ItemStorage storage)
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
            storage.Priority = priority;
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

    public bool RemoveItem(Thing item)
    {
        return SortedStorages.Any(storage => storage.Remove(item));
    }

    public IEnumerable<Thing> GetVirtualItems()
    {
        return Storages.SelectMany(storage => storage.GetVirtualItems());
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
            sum += storage.Count;
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)sum;
    }

    public bool Contains(Thing item)
    {
        return SortedStorages.Any(storage => storage.Contains(item));
    }

    public bool Contains(ThingDef def)
    {
        return SortedStorages.Any(storage => storage.Contains(def));
    }
    
    public Building_AccessInterface? GetClosestAccessInterface(IntVec3 position, float searchRadius = float.MaxValue)
    {
        Building_AccessInterface? closest = null;

        foreach (var @interface in AccessInterfaces)
        {
            if (!@interface.IsForbidden(Faction.OfPlayer))
            {
                var distSq = position.DistanceToSquared(@interface.Position);
                if (distSq < searchRadius)
                {
                    searchRadius = distSq;
                    closest = @interface;
                }
            }
        }

        return closest;
    }
}