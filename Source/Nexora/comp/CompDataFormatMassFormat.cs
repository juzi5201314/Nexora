using System.Diagnostics;
using RimWorld;
using Verse;

namespace Nexora.comp;

public class CompDataFormatMassFormat : CompDataFormat
{
    internal decimal CachedMass = 0;
    internal bool dirty = true;

    public decimal Mass(ItemStorage storage)
    {
        if (dirty)
        {
            CachedMass = 0;
            foreach (var thing in storage.Container)
            {
                CachedMass += thing.stackCount * (decimal)thing.GetStatValue(StatDefOf.Mass, cacheStaleAfterTicks: 600);
            }

            dirty = false;
        }

        return CachedMass;
    }

    public CompDataFormatMassFormat()
    {
    }

    public override void OnAdd(ItemStorage storage)
    {
        dirty = true;
    }

    public override void OnRemove(ItemStorage storage, Thing item, int count)
    {
        dirty = true;
    }

    public override int GetCountCanAccept(ItemStorage storage, Thing item)
    {
        var single = item.GetStatValue(StatDefOf.Mass);
        var remaining = Math.Max(Props.Value - Mass(storage), 0);
        if (single <= 0)
        {
            return int.MaxValue;
        }

        return (int)decimal.Floor(remaining / (decimal)single);
    }

    public override IEnumerable<string> GetExtraInspectString(ItemStorage storage)
    {
        yield return $"{"Capacity".Translate()}: {Mass(storage):F3} / {Props.Value:F2} kg";
    }
}