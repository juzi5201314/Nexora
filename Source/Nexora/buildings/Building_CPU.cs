using System.Text;
using Nexora.comp;
using Nexora.network;
using Verse;

namespace Nexora.buildings;

public class Building_CPU : Building
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();
    public CompWorkrate WorkrateComp => GetComp<CompWorkrate>();

    public uint TotalWorkrate = 0;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (WorkrateComp == null)
        {
            Log.Error($"CPU {def.defName} missing workrate");
            return;
        }

        TotalWorkrate = WorkrateComp.Props.ops;
        Network.Connect(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (WorkrateComp != null)
            Network.Disconnect(this);
        base.DeSpawn(mode);
    }

    public override string GetInspectString()
    {
        var sb = new StringBuilder(base.GetInspectString());
        sb.AppendInNewLine($"Provided workrate: {WorkrateComp.Props.ops} ops");
        sb.AppendInNewLine($"Provided maxDevices: {WorkrateComp.Props.maxDevices} ops");
        return sb.ToString();
    }
}