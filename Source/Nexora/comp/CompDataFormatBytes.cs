using UnityEngine;
using Verse;

namespace Nexora.comp;

public class CompDataFormatBytes : CompDataFormat
{
    public override CompProperties_DataFormat Props => (CompProperties_DataFormatBytes)props;
    public CompProperties_DataFormatBytes Props1 => (CompProperties_DataFormatBytes)props;

    private decimal totalBytesCache = 0;
    private Dictionary<Thing, int> countCanAcceptCache = new();

    private const decimal bytesPreStack = 16;
    private const decimal bytesPreThing = 0.5m;

    public CompDataFormatBytes()
    {
    }

    public override int GetCountCanAccept(ItemStorage storage, Thing item)
    {
        if (countCanAcceptCache.TryGetValue(item, out var count0))
        {
            return count0;
        }

        var rem = Props.Value - totalBytesCache;

        if (IsNewStack(storage, item))
        {
            rem -= bytesPreStack;
        }

        if (rem <= 0)
        {
            return 0;
        }

        var count = (int)Math.Min(rem / bytesPreThing, int.MaxValue);
        countCanAcceptCache.SetOrAdd(item, count);

        return count;
    }

    public override IEnumerable<string> GetExtraInspectString(ItemStorage storage)
    {
        yield return $"{"Capacity".Translate()}: {Props1.ToString(totalBytesCache)} / {Props1.ToString(Props1.Value)}";
    }

    public decimal CalcTotalBytes(ItemStorage storage)
    {
        var things = storage.GetAllItems().Select(t => (decimal)t.stackCount).Sum();
        var num = Math.Ceiling(storage.Count * bytesPreStack + things * bytesPreThing);
        return num;
    }

    public bool IsNewStack(ItemStorage storage, Thing item)
    {
        if (!storage.IndexTable.TryGetValue(item.def, out var dict))
        {
            return true;
        }

        foreach (var (key, _) in dict)
        {
            if (item.CanStackWith(key))
            {
                return false;
            }
        }

        return true;
    }

    public override void OnChange(ItemStorage storage)
    {
        countCanAcceptCache.Clear();
        totalBytesCache = CalcTotalBytes(storage);
    }
}