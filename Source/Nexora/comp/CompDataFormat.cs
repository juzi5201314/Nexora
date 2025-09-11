using Verse;

namespace Nexora.comp;

public abstract class CompDataFormat: ThingComp
{
    public CompProperties_DataFormat Props => (CompProperties_DataFormat)props;

    public virtual void OnAdd(ItemStorage storage) {}
    public virtual void OnRemove(ItemStorage storage, Thing item, int count) {}
    public abstract int GetCountCanAccept(ItemStorage storage, Thing item);
    public abstract IEnumerable<string> GetExtraInspectString(ItemStorage storage);
}