using System.Reflection.Emit;
using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(FoodUtility))]
public static class FoodUtilityPatch
{
    [HarmonyPatch(nameof(FoodUtility.BestFoodSourceOnMap))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> BestFoodSourceOnMap(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);

        matcher.MatchStartForward(new CodeMatch(OpCodes.Call,
                AccessTools.Method(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")))
            .ThrowIfInvalid("BestFoodSourceOnMap failed to find match")
            .RemoveInstruction()
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_1))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(FoodUtilityPatch), "SpawnedFoodSearchInnerScan")));
        return matcher.Instructions();
    }

    public static Thing? SpawnedFoodSearchInnerScan(Pawn eater, IntVec3 root, List<Thing> searchSet, PathEndMode peMode,
        TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null,
        ThingRequest request = default)
    {
        // 先调用原版方法，防止错过其他mod的更改，比如DigitalStorageUnit
        var result = Traverse.Create(typeof(FoodUtility)).Method("SpawnedFoodSearchInnerScan", eater, root, searchSet,
            peMode, traverseParams, maxDistance, validator).GetValue<Thing>();
        var network = eater.Map.GetComponent<LocalNetwork>();
        var inter = network.GetClosestAccessInterface(root, maxDistance, peMode, traverseParams);
        if (inter == null) return null;

        // copy from `FoodUtility.SpawnedFoodSearchInnerScan`
        var thing = result;
        var num3 = float.MinValue;
        foreach (var search in network.GetItemByRequest(request))
        {
            var lengthManhattan = (root - search.Position).LengthManhattan;
            if (lengthManhattan <= maxDistance)
            {
                var num4 = FoodUtility.FoodOptimality(eater, search, FoodUtility.GetFinalIngestibleDef(search),
                    lengthManhattan);
                if (num4 >= num3 && (validator == null || validator(search)))
                {
                    thing = search;
                    num3 = num4;
                }
            }
        }

        return thing;
    }
}