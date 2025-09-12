using Nexora.utils.pooled;
using Verse;
using Verse.Sound;

namespace Nexora.utils;

public static class GenPlace
{
    public static bool TryPlaceItemWithStacking(
        Thing thing,
        IntVec3 dropCell,
        Map map,
        out Thing resultingThing,
        bool playDropSound = true)
    {
        if (map == null)
        {
            Log.Error($"Dropped {thing} in a null map.");
            resultingThing = null;
            return false;
        }

        if (!dropCell.InBounds(map))
        {
            Log.Error($"Dropped {thing} out of bounds at {dropCell.ToString()}");
            resultingThing = null;
            return false;
        }

        if (thing.def.destroyOnDrop)
        {
            thing.Destroy();
            resultingThing = null;
            return true;
        }

        if (!TryPlaceDirect(thing, dropCell, thing.def.defaultPlacingRot, map, out resultingThing))
        {
            return false;
        }

        if (playDropSound && thing.def.soundDrop != null)
            thing.def.soundDrop.PlayOneShot(new TargetInfo(dropCell, map));
        return true;
    }

    private static bool TryPlaceDirect(Thing thing,
        IntVec3 loc,
        Rot4 rot,
        Map map,
        out Thing resultingThing)
    {
        resultingThing = null;
        using var cellThings = new PooledList<Thing>();
        cellThings.Inner.AddRange(loc.GetThingList(map));
        cellThings.Inner.Sort((Comparison<Thing>)((lhs, rhs) => rhs.stackCount.CompareTo(lhs.stackCount)));
        // fuck the auto stacking

        int num1;
        if (thing.def.category == ThingCategory.Item)
        {
            var num2 = cellThings.Count(cellThing => cellThing.def.category == ThingCategory.Item);
            num1 = loc.GetMaxItemsAllowedInCell(map) - num2;
        }
        else
            num1 = thing.stackCount + 1;

        if (num1 <= 0 && thing.def.stackLimit <= 1)
            num1 = 1;
        for (var index = 0; index < num1; ++index)
        {
            var newThing = thing.stackCount <= thing.def.stackLimit ? thing : thing.SplitOff(thing.def.stackLimit);
            resultingThing = GenSpawn.Spawn(newThing, loc, map, rot);
            if (newThing == thing)
                return true;
        }

        return false;
    }
}