using LudeonTK;
using Nexora.network;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nexora;

public static class DebugActions
{
    [DebugAction("Nexora", "RandomAddItemToNetwork", actionType = DebugActionType.Action,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void DebugAction_RandomAddItemToNetwork()
    {
        var net = Find.CurrentMap.GetComponent<LocalNetwork>();
        if (net == null)
        {
            Log.Error("No network found");
            return;
        }

        RandomAddItemToStorage(net);
    }

    public static void RandomAddItemToStorage(IItemInterface itemInterface)
    {
        var defs = DefDatabase<ThingDef>
            .AllDefs
            .AsParallel()
            .OrderBy(_ => Rand.Value)
            .ToList();

        foreach (var def in defs.Where(def => def.category == ThingCategory.Item).Where(def => def.IsWeapon).Take(5))
        {
            ThingDef? stuff = null;
            if (def.MadeFromStuff)
            {
                stuff = GenStuff.RandomStuffFor(def);
            }

            var weapon = ThingMaker.MakeThing(def, stuff);
            if (weapon.HasComp<CompQuality>())
            {
                weapon.TryGetComp<CompQuality>()
                    .SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
            }

            itemInterface.TryAddItem(weapon);
        }

        foreach (var def in defs
                     .Where(def => def.category == ThingCategory.Item)
                     .Where(def => def.IsNutritionGivingIngestible)
                     .Where(def => !def.IsCorpse)
                     .Where(def => !def.IsDrug)
                     .Take(5))
        {
            var food = ThingMaker.MakeThing(def);
            food.stackCount = Rand.RangeInclusive(1, def.stackLimit);
            itemInterface.TryAddItem(food);
        }

        foreach (var def in defs
                     .Where(def => def.category == ThingCategory.Building)
                     .Where(def => def.Minifiable)
                     .Take(5))
        {
            ThingDef? stuff = null;
            if (def.MadeFromStuff)
            {
                stuff = GenStuff.RandomStuffFor(def);
            }

            var building = ThingMaker.MakeThing(def, stuff);
            if (building.HasComp<CompQuality>())
            {
                building.TryGetComp<CompQuality>()
                    .SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
            }

            itemInterface.TryAddItem(building.MakeMinified());
        }

        foreach (var def in defs
                     .Where(def => def.category == ThingCategory.Item)
                     .Where(def => def.IsStuff)
                     .Take(5))
        {
            var stuff = ThingMaker.MakeThing(def);
            stuff.stackCount = Rand.RangeInclusive(1, def.stackLimit);
            itemInterface.TryAddItem(stuff);
        }

        /*foreach (var corpse in DefDatabase<PawnKindDef>.AllDefs
                     .OrderBy(_ => Rand.Value)
                     .Select(def => PawnGenerator.GeneratePawn(def, null, Find.CurrentMap.Tile))
                     .Select(pawn =>
                     {
                         pawn.relations?.ClearAllRelations();
                         pawn.inventory?.DestroyAll();
                         pawn.equipment?.DestroyAllEquipment();
                         pawn.apparel?.DestroyAll();
                         return pawn;
                     })
                     .Select(pawn => pawn.MakeCorpse(null, null))
                     .Where(corpse => !corpse.Bugged)
                     .Take(5))
        {
            itemInterface.TryAddItem(corpse);
        }*/
    }
}