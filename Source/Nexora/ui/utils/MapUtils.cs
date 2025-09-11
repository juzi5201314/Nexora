using Verse;

namespace Nexora.ui.utils;

public static class MapUtils
{
    // 我不清楚跟GetEdifice有什么区别，但是一个格子上有可能有多个建筑，而EdificeGrid只有一个，我不清楚是什么。
    public static T? GetFirstBuilding<T>(this Map map, IntVec3 cell) where T : Building
    {
        foreach (var thing in map.thingGrid.ThingsAt(cell))
        {
            if (thing is T t)
            {
                return t;
            }
        }

        return null;
    }
}