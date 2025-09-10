using Verse;

namespace Nexora.network;

public interface IItemInterface
{
    public abstract IEnumerable<Thing> GetVirtualItems();

    public abstract int TryAddItem(Thing item);

    public abstract int GetCountCanAccept(Thing item);
    
    public abstract int Count();

    public abstract bool Contains(Thing item);

    public abstract LocalNetwork Network();
}