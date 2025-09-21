using Verse;

namespace Nexora.comp;

public class CompProperties_Workrate : CompProperties
{
    public CompProperties_Workrate() => compClass = typeof(CompWorkrate);
    
    public uint ops = 1;
    public uint maxDevices = 1;
}