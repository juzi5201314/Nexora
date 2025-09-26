using Verse;

namespace Nexora.comp;

public class CompDataFormatType: CompDataFormat
{
    public override int GetCountCanAccept(ItemStorage storage, Thing item)
    {
        var thingCount = storage.Count;
        return thingCount < Props.Value ? int.MaxValue : 0;
    }

    public override IEnumerable<string> GetExtraInspectString(ItemStorage storage)
    {
        yield return $"{"Capacity".Translate()}: {storage.Count} / {Props.Value} types";
    }
}