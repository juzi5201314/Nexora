using System.Reflection.Emit;
using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch(typeof(WorkGiver_DoBill))]
public static class WorkGiver_DoBillPatch
{
    // 在查找工作清单原材料时将存储网络中的物品加入到可用物品列表中
    [HarmonyPatch("TryFindBestIngredientsHelper")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TryFindBestIngredientsHelper(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        //      int regionsProcessed = 0;
        //393-> WorkGiver_DoBill.processedThings.AddRange<Thing>(WorkGiver_DoBill.relevantThings);
        //      if (foundAllIngredientsAndChoose(WorkGiver_DoBill.relevantThings))
        //      return true;
        matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "processedThings")),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "relevantThings")),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GenCollection), "AddRange", [
                    typeof(HashSet<Thing>),
                    typeof(List<Thing>)
                ]))
            )
            .ThrowIfInvalid("TryFindBestIngredientsHelper failed to find match");
        matcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_S, 4),
            new CodeInstruction(OpCodes.Ldarg_S, 6),
            new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(WorkGiver_DoBillPatch), nameof(AddThingRelevantThings)))
        );

        return matcher.Instructions();
    }

    public static void AddThingRelevantThings(Predicate<Thing> thingValidator, List<IngredientCount> ingredients,
        Pawn pawn, Thing billGiver, float searchRadius)
    {
        var network = pawn.Map.GetComponent<LocalNetwork>();
        var inter = network.GetClosestAccessInterface(billGiver.Position, searchRadius,
            traverseParams: TraverseParms.For(pawn));
        if (inter is null)
        {
            return;
        }

        var processedThings = Traverse.Create<WorkGiver_DoBill>().Field("processedThings").GetValue<HashSet<Thing>>();
        var newRelevantThings = Traverse.Create<WorkGiver_DoBill>().Field("newRelevantThings").GetValue<List<Thing>>();
        foreach (var item in network.GetVirtualItems())
        {
            if (pawn.CanReserve(item) && !item.IsForbidden(pawn) && thingValidator(item) && processedThings.Add(item))
            {
                newRelevantThings.Add(item);
            }
        }
    }
}