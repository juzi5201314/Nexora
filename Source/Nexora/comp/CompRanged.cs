using Verse;

namespace Nexora.comp;

public class CompRanged : ThingComp
{
    private CompProperties_Ranged Props => (CompProperties_Ranged)props;

    public override void PostDrawExtraSelectionOverlays()
    {
        GenDraw.DrawRadiusRing(parent.Position, Props.radius);
    }

    public IEnumerable<IntVec3> CellInRange()
    {
        var num = GenRadial.NumCellsInRadius(Props.radius);
        for (var index = 0; index < num; ++index)
        {
            yield return parent.Position + GenRadial.RadialPattern[index];
        }
    }
}