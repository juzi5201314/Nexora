using Nexora.network;
using PipeSystem;
using Verse;

namespace Nexora.VEF_PipeSystem;

public class FakeCompResourceStorage : CompResourceStorage
{
    public LocalNetwork Network;
    public Thing Resource;
    
    public override void CompTickInterval(int delta)
    {
    }

    public override void CompTick()
    {
    }

    public override void CompTickRare()
    {
    }

    public override void CompTickLong()
    {
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
    }

    public override void PostDraw()
    {
    }

    public override void PostExposeData()
    {
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
    }

    public override string CompInspectStringExtra() => "";

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield break;
    }

    public new float AmountStored => 10086;
    public new float AmountStoredPct => 1.0f;
    public new float AmountCanAccept => 0;

    public new void AddResource(float amount)
    {
    }

    public new void DrawResource(float amount)
    {
    }

    public new void Empty()
    {
    }
}