using Verse;

namespace Nexora.comp;

public class CompProperties_Ranged : CompProperties
{
    public float radius = 0f;

    public CompProperties_Ranged() => compClass = typeof(CompRanged);
}