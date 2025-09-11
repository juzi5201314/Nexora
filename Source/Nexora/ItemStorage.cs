using System.Collections.Concurrent;
using System.Diagnostics;
using Nexora.comp;
using Nexora.network;
using Nexora.utils;
using RimWorld;
using UnityEngine;
using UnityEngine.Pool;
using Verse;

namespace Nexora;

public class ItemStorage(Building_LocalStorage owner) : ThingOwner(owner), IItemInterface, IExposable
{
    internal readonly Dictionary<ThingDef, Dictionary<Thing, int>> IndexTable = new();
    internal List<Thing> Container = [];
    internal int Priority = 0;

    internal new readonly Building_LocalStorage Owner = owner;

    public LocalNetwork Network() => Owner.Network;

    int IItemInterface.Count() => Count;

    public int GetCountCanAccept(Thing item)
    {
        return GetCountCanAccept(item, true);
    }

    public IEnumerable<Thing> GetVirtualItems()
    {
        return Container;
    }

    public IEnumerable<Thing> GetItemsByDef(ThingDef def)
    {
        if (IndexTable.TryGetValue(def, out var dict))
        {
            foreach (var (thing, _) in dict)
            {
                yield return thing;
            }
        }
    }

    public void AddItems(IEnumerable<Thing> things)
    {
        foreach (var thing in things)
        {
            TryAddItem(thing);
        }
    }

    public int TryAddItem(Thing item)
    {
        if (item == null)
        {
            Log.Warning("Tried to add null item to ThingOwner.");
            return -1;
        }

        if (item.def.category != ThingCategory.Item)
        {
            Log.Error($"[Nexora] try add ({item.def.category}){item.def.defName} to storage");
            return -1;
        }

        if (item is Corpse { Bugged: true } corpse)
        {
            Log.Warning($"[Nexora] cannot add bugged corpse to storage. def: `{corpse.def.LabelCap}`");
            return -1;
        }

        var num = Math.Min(GetCountCanAccept(item), item.stackCount);
        if (num <= 0)
        {
            return 0;
        }

        if (item.holdingOwner != null)
        {
            if (item.holdingOwner == this)
            {
                return item.stackCount;
            }

            if (item.holdingOwner is not ItemStorage)
            {
                return item.holdingOwner.TryTransferToContainer(item, this, item.stackCount);
            }
        }

        if (!IndexTable.ContainsKey(item.def))
        {
            IndexTable.Add(item.def, new Dictionary<Thing, int>());
        }

        IndexTable.TryGetValue(item.def, out var dict);

        var other = item.SplitOff(num);
        if (other.Spawned)
        {
            other.DeSpawnOrDeselect();
        }

        foreach (var thing in dict!.Keys)
        {
            if (int.MaxValue - thing.stackCount >= num && thing.CanStackWith(other))
            {
                thing.TryAbsorbStack(other, false);
                if (other.Destroyed || other.stackCount == 0)
                {
                    Owner.CompDataFormat.OnAdd(this);
                    return num;
                }
            }
        }

        other.holdingOwner = this;
        dict.Add(other, Container.Count);
        Container.Add(other);
        Owner.CompDataFormat.OnAdd(this);
        return num;
    }

    public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
    {
        return Owner.CompDataFormat.GetCountCanAccept(this, item);
    }

    public new void DoTick()
    {
    }

    public new void ExposeData()
    {
        Scribe_Collections.Look(ref Container, "Container", LookMode.Deep);
        Scribe_Values.Look(ref Priority, "Priority");

        switch (Scribe.mode)
        {
            case LoadSaveMode.PostLoadInit:
            {
                Container ??= [];
                Container.RemoveAll(thing =>
                    thing is null or MinifiedThing { InnerThing: null } or Corpse { InnerPawn: null });
                for (var i = 0; i < Container.Count; i++)
                {
                    var thing = Container[i];
                    thing.holdingOwner = this;
                    if (!IndexTable.ContainsKey(thing.def))
                    {
                        IndexTable.Add(thing.def, new Dictionary<Thing, int>());
                    }

                    IndexTable.TryGetValue(thing.def, out var dict);
                    dict!.Add(thing, i);
                }

                break;
            }
            case LoadSaveMode.Saving:
            {
                break;
            }
        }
    }

    public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
    {
        var count1 = Math.Min(item.stackCount, count);
        var count2 = Math.Min(GetCountCanAccept(item), count1);
        var other = item.SplitOff(count2);
        return TryAddItem(other);
    }

    public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
    {
        return TryAddItem(item) > 0;
    }

    public override int IndexOf(Thing? item)
    {
        if (item is null || !IndexTable.TryGetValue(item.def, out var dict))
        {
            return -1;
        }

        return dict.TryGetValue(item, -1);
    }

    public new bool Contains(ThingDef def)
    {
        return IndexTable.ContainsKey(def);
    }

    public override bool Remove(Thing? item)
    {
        if (item is null || Container.Empty() || !IndexTable.TryGetValue(item.def, out var thingDict) ||
            !thingDict.TryGetValue(item, out var idx))
        {
            return false;
        }

        if (item.holdingOwner == this)
        {
            item.holdingOwner = null;
        }

        thingDict.Remove(item);
        if (thingDict.Count <= 0)
        {
            IndexTable.Remove(item.def);
        }

        var last = Container.Count - 1;
        if (idx != last)
        {
            var lastThing = Container[last];

            Container[idx] = lastThing;
            if (IndexTable.TryGetValue(lastThing.def, out var lastDict))
            {
                lastDict.SetOrAdd(lastThing, idx);
            }
        }

        Container.RemoveAt(last);
        Owner.CompDataFormat.OnRemove(this, item, item.stackCount);
        return true;
    }

    public override int Count => Container.Count;

    protected override Thing GetAt(int index) => Container[index];
}