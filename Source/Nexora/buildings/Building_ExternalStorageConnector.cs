using Nexora.comp;
using Nexora.network;
using Nexora.ui;
using Nexora.ui.utils;
using RimWorld;
using Verse;

namespace Nexora.buildings;

public class Building_ExternalStorageConnector : Building
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    public HashSet<IntVec3> CellsInRange = [];
    public readonly HashSet<Building_Storage> ExternalStorages = [];

    public int Priority = 0;

    public Building_ExternalStorageConnector()
    {
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        CellsInRange = GetComp<CompRanged>().CellInRange().ToHashSet();
        Map.events.BuildingSpawned += OnBuildingSpawned;
        Map.events.BuildingDespawned += OnBuildingDespawned;
        foreach (var building in CellsInRange.Select(vec3 => Map.GetFirstBuilding<Building>(vec3)).OfType<Building>())
        {
            OnBuildingSpawned(building);
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.events.BuildingSpawned -= OnBuildingSpawned;
        Map.events.BuildingDespawned -= OnBuildingDespawned;
        foreach (var building in CellsInRange.Select(vec3 => Map.GetFirstBuilding<Building>(vec3)).OfType<Building>())
        {
            OnBuildingDespawned(building);
        }

        base.DeSpawn(mode);
    }

    private void OnBuildingSpawned(Building building)
    {
        if (building is not Building_Storage storage || building is Building_LocalStorage ||
            !CellsInRange.Contains(building.Position))
        {
            return;
        }

        ExternalStorages.Add(storage);
    }

    private void OnBuildingDespawned(Building building)
    {
        if (building is not Building_Storage storage || building is Building_LocalStorage ||
            !CellsInRange.Contains(building.Position))
        {
            return;
        }

        ExternalStorages.Remove(storage);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Priority, "Priority");
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        foreach (var building in ExternalStorages)
        {
            GenDraw.DrawTargetHighlightWithLayer(building.Position, AltitudeLayer.MetaOverlays);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        yield return new Command_Settle()
        {
            defaultLabel = "Set Priority".Translate(),
            icon = Assets.Priority,
            action = () =>
            {
                Find.WindowStack.Add(new Dialog_SliderWithInput(
                    val => $"Storage Priority: {val}",
                    -50,
                    100,
                    value => { Network.ChangeProperty(this, value); },
                    Priority
                ));
            }
        };
    }
}