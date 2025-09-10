using Verse;

namespace Nexora.comp;

public class DataFormatComp_Unlimited : DataFormatComp
{
    public override int GetCountCanAccept(ItemStorage storage, Thing item) => int.MaxValue;

    public override IEnumerable<string> GetExtraInspectString(ItemStorage storage)
    {
        yield return $"{"Capacity".Translate()}: It can store as many items as this mod has bugs";
    }
}