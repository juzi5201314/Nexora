using Nexora.comp;
using Nexora.network;
using Nexora.ui.utils;
using RimWorld;
using Verse;

namespace Nexora.buildings;

public class Building_ExternalStorageConnector : Building
{
    public LocalNetwork Network => Map.GetComponent<LocalNetwork>();

    public HashSet<IntVec3> CellsInRange = [];
    public readonly HashSet<Building_Storage> ExternalStorages = [];

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

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        foreach (var building in ExternalStorages)
        {
            GenDraw.DrawTargetHighlightWithLayer(building.Position, AltitudeLayer.MetaOverlays);
        }
    }
}