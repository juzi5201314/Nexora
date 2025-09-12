using Verse;

namespace Nexora.network;

public interface IItemInterface
{
    public abstract IEnumerable<Thing> GetVirtualItems();
    public abstract IEnumerable<Thing> GetExternalItems();
    public abstract IEnumerable<Thing> GetAllItems();

    public abstract int TryAddItem(Thing item);

    public abstract int GetCountCanAccept(Thing item);
    
    public abstract int Count();
    public abstract int Priority();

    public abstract bool Contains(Thing item);

    public abstract LocalNetwork Network();
}