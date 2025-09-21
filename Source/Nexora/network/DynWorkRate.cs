using Verse;

namespace Nexora.network;

public class DynWorkRate(int value, int priority)
{
    public int Value = value;
    public int Expected = value;
    public bool Released = false;
    public int Priority = priority;

    public bool Low => Value != Expected;
}