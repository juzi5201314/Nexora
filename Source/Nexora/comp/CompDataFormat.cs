using Verse;

namespace Nexora.comp;

public abstract class CompDataFormat: ThingComp
{
    public virtual CompProperties_DataFormat Props => (CompProperties_DataFormat)props;

    public virtual void OnChange(ItemStorage storage) {}
    public abstract int GetCountCanAccept(ItemStorage storage, Thing item);
    public abstract IEnumerable<string> GetExtraInspectString(ItemStorage storage);
}