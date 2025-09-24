using HarmonyLib;
using Nexora.network;
using PipeSystem;
using RimWorld;
using Verse;

namespace Nexora.VEF_PipeSystem;

public static class CompResourceStoragePatch
{
    public static bool AmountCanAccept(CompResourceStorage __instance, ref float __result)
    {
        if (__instance is not FakeCompResourceStorage fakeStorage)
        {
            return true;
        }

        __result = fakeStorage.Network.GetCountCanAccept(fakeStorage.Resource);
        return false;
    }

    public static bool AmountStored(CompResourceStorage __instance, ref float __result)
    {
        if (__instance is not FakeCompResourceStorage fakeStorage)
        {
            return true;
        }

        __result = fakeStorage.Network.GetItemsByDef(fakeStorage.Resource.def)
            .Select(t => t.stackCount)
            .Sum();

        return false;
    }

    public static bool AmountStoredPct(CompResourceStorage __instance, ref float __result)
    {
        if (__instance is not FakeCompResourceStorage fakeStorage)
        {
            return true;
        }

        __result = 0f;
        return false;
    }
    
    public static bool AddResource(CompResourceStorage __instance, float amount)
    {
        if (__instance is not FakeCompResourceStorage fakeStorage)
        {
            return true;
        }

        var resources = ThingMaker.MakeThing(fakeStorage.Resource.def);
        resources.stackCount = (int)amount;
        fakeStorage.Network.TryAddItem(resources);
        return false;
    }
    
    public static bool DrawResource(CompResourceStorage __instance, float amount)
    {
        if (__instance is not FakeCompResourceStorage fakeStorage)
        {
            return true;
        }

        var rem = (int)amount;
        foreach (var thing in fakeStorage.Network.GetItemsByDef(fakeStorage.Resource.def))
        {
            var num = Math.Min(thing.stackCount, rem);
            thing.SplitOff(num);
            rem -= num;

            if (rem <= 0)
            {
                break;
            }
        }

        return false;
    }
}

public static class PipeNetMakerPatch
{
    public static void MakePipeNet(ref PipeNet __result, Map map)
    {
        var thingDef = TryBind(__result);
        if (thingDef != null)
        {
            __result.storages.Add(new FakeCompResourceStorage()
            {
                PipeNet = __result,
                Network = map.GetComponent<LocalNetwork>(),
                Resource = ThingMaker.MakeThing(thingDef),
                parent = new ThingWithComps(),
            });
        }
    }

    private static ThingDef? TryBind(PipeNet pipeNet)
    {
        switch (pipeNet.def.defName)
        {
            case "VCHE_ChemfuelNet":
                return ThingDefOf.Chemfuel;
            case "VNPE_NutrientPasteNet":
                return ThingDefOf.MealNutrientPaste;
            default:
                return null;
        }
    }
}