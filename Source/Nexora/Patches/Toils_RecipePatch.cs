using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nexora.network;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora.Patches;

[HarmonyPatch]
public static class Toils_RecipePatch
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        var target = typeof(Toils_Recipe).GetNestedType("<>c__DisplayClass3_0", AccessTools.all)
            .GetMethod("<FinishRecipeAndStartStoringProduct>b__1", AccessTools.all);
        if (target == null)
        {
            Log.Error("Toils_RecipePatch.TargetMethod() failed");
        }

        return target!;
    }
    
    // 使工作清单中的"放到最佳存储区"可以查找到访问接口所在cell
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FinishRecipeAndStartStoringProduct(
        IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(new CodeMatch(OpCodes.Call,
                AccessTools.Method(typeof(StoreUtility), "TryFindBestBetterStoreCellFor")))
            .ThrowIfInvalid("FinishRecipeAndStartStoringProduct failed to find match")
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(Toils_RecipePatch), "TryFindBestBetterStoreCellFor2")));
        return matcher.Instructions();
    }

    public static bool TryFindBestBetterStoreCellFor2(
        Thing t,
        Pawn carrier,
        Map map,
        StoragePriority currentPriority,
        Faction faction,
        out IntVec3 foundCell,
        bool needAccurateResult = true)
    {
        var result = StoreUtility.TryFindBestBetterStoreCellFor(t, carrier, map, currentPriority, faction,
            out foundCell, needAccurateResult);
        var network = map.GetComponent<LocalNetwork>();
        foreach (var inter in network.GetAccessInterfaces(t.SpawnedOrAnyParentSpawned
                     ? t.PositionHeld
                     : carrier.PositionHeld))
        {
            var priority = true;
            if (foundCell.IsValid)
            {
                var group = map.haulDestinationManager.SlotGroupAt(foundCell);
                var cellPriority = group?.Settings?.Priority ?? StoragePriority.Unstored;
                priority = inter.GetStoreSettings().Priority >= cellPriority;
            }

            if (inter.HaulDestinationEnabled && priority && inter.Accepts(t))
            {
                foundCell = inter.Position;
                return true;
            }
        }

        return result;
    }
}