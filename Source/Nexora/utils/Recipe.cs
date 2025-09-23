using HarmonyLib;
using Nexora.network;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nexora.utils;

public static class Recipe
{
    public static bool TryFindAndTakeBestBillIngredients(Bill_Production bill, LocalNetwork network,
        out List<Thing> ingredients, out List<IngredientCount> missingIngredients)
    {
        ingredients = [];
        missingIngredients = [];
        var ingredientCounts = new List<ThingCount>();
        var things = network.GetAllItems().Where(t => IsUsableIngredient(t, bill)).ToList();
        var tryFindBestBillIngredientsInSet = Traverse.Create<WorkGiver_DoBill>().Method(
                "TryFindBestBillIngredientsInSet", things, bill,
                ingredientCounts, IntVec3.Zero, true, missingIngredients)
            .GetValue<bool>();
        if (tryFindBestBillIngredientsInSet)
        {
            foreach (var thingCount in ingredientCounts)
            {
                ingredients.Add(thingCount.Thing.SplitOff(thingCount.Count));
            }
        }

        return tryFindBestBillIngredientsInSet;
    }

    private static bool IsUsableIngredient(Thing t, Bill bill)
    {
        if (!bill.IsFixedOrAllowedIngredient(t))
            return false;
        foreach (var ingredient in bill.recipe.ingredients)
        {
            if (ingredient.filter.Allows(t))
                return true;
        }

        return false;
    }

    public static Thing? GetDominantIngredient(RecipeDef recipeDef, List<Thing> ingredients)
    {
        if (ingredients.NullOrEmpty())
            return null;
        if (recipeDef.productHasIngredientStuff)
            return ingredients[0];
        return
            recipeDef.products.Any(
                (Predicate<ThingDefCountClass>)(x => x.thingDef.MadeFromStuff)) ||
            recipeDef.unfinishedThingDef is { MadeFromStuff: true }
                ? ingredients.Where((Func<Thing, bool>)(x => x.def.IsStuff))
                    .RandomElementByWeight((Func<Thing, float>)(x => x.stackCount))
                : ingredients.RandomElementByWeight((Func<Thing, float>)(x => x.stackCount));
    }

    public static IEnumerable<Thing> MakeRecipeProducts(
        RecipeDef recipeDef,
        Pawn worker,
        List<Thing> ingredients,
        Thing? dominantIngredient,
        IBillGiver billGiver,
        Precept_ThingStyle precept,
        ThingStyleDef? style,
        QualityCategory quality,
        int? overrideGraphicIndex)
    {
        var efficiency = 1f;
        if (recipeDef.workTableEfficiencyStat != null && billGiver is Building_WorkTable thing1)
            efficiency *= thing1.GetStatValue(recipeDef.workTableEfficiencyStat);
        int i;
        if (recipeDef.products != null)
        {
            for (i = 0; i < recipeDef.products.Count; ++i)
            {
                ThingDefCountClass product = recipeDef.products[i];
                var def = product.thingDef.MadeFromStuff ? dominantIngredient?.def : null;
                Thing thing2 = ThingMaker.MakeThing(product.thingDef, def);
                thing2.stackCount = Mathf.CeilToInt((float)product.count * efficiency);
                if (dominantIngredient != null && recipeDef.useIngredientsForColor)
                    thing2.SetColor(dominantIngredient.DrawColor, false);
                CompIngredients comp = thing2.TryGetComp<CompIngredients>();
                if (comp != null)
                {
                    for (int index = 0; index < ingredients.Count; ++index)
                        comp.RegisterIngredient(ingredients[index].def);
                }

                thing2.Notify_RecipeProduced(worker);
                yield return PostProcessProduct(thing2, recipeDef, worker, precept, style, quality,
                    overrideGraphicIndex);
            }
        }

        if (recipeDef.specialProducts != null)
        {
            for (i = 0; i < recipeDef.specialProducts.Count; ++i)
            {
                for (int j = 0; j < ingredients.Count; ++j)
                {
                    Thing ingredient = ingredients[j];
                    switch (recipeDef.specialProducts[i])
                    {
                        case SpecialProductType.Butchery:
                            foreach (Thing butcherProduct in ingredient.ButcherProducts(worker, efficiency))
                                yield return PostProcessProduct(butcherProduct, recipeDef, worker, precept,
                                    style, quality, overrideGraphicIndex);
                            break;
                        case SpecialProductType.Smelted:
                            foreach (Thing smeltProduct in ingredient.SmeltProducts(efficiency))
                                yield return PostProcessProduct(smeltProduct, recipeDef, worker, precept,
                                    style, quality, overrideGraphicIndex);
                            break;
                    }
                }
            }
        }

        if (recipeDef.Worker is RecipeWorker_DigitalMining)
        {
            foreach (var thing in MakeDigitalMiningProduct())
            {
                yield return thing;
            }
        }
    }

    private static Thing PostProcessProduct(
        Thing product,
        RecipeDef recipeDef,
        Pawn worker,
        Precept_ThingStyle precept,
        ThingStyleDef? style,
        QualityCategory quality,
        int? overrideGraphicIndex)
    {
        CompQuality comp1 = product.TryGetComp<CompQuality>();
        if (comp1 != null)
        {
            if (recipeDef.workSkill == null)
                Log.Error(recipeDef?.ToString() + " needs workSkill because it creates a product with a quality.");
            comp1.SetQuality(quality, ArtGenerationContext.Colony);
        }

        CompArt comp2 = product.TryGetComp<CompArt>();
        if (comp2 != null)
        {
            comp2.JustCreatedBy(worker);
            if (comp1 != null && comp1.Quality >= QualityCategory.Excellent)
                TaleRecorder.RecordTale(TaleDefOf.CraftedArt, (object)worker, (object)product);
        }

        if (comp1 != null)
            QualityUtility.SendCraftNotification(product, worker);
        if (precept != null)
            product.StyleSourcePrecept = precept;
        else if (style != null)
            product.StyleDef = style;
        else if (!product.def.randomStyle.NullOrEmpty<ThingStyleChance>() && Rand.Chance(product.def.randomStyleChance))
            product.SetStyleDef(product.def.randomStyle
                .RandomElementByWeight<ThingStyleChance>((Func<ThingStyleChance, float>)(x => x.Chance)).StyleDef);
        product.overrideGraphicIndex = overrideGraphicIndex;
        if (product.def.Minifiable)
            product = (Thing)product.MakeMinified();
        return product;
    }

    private static List<ThingDef> MetallicStuffs = [];

    private static IEnumerable<Thing> MakeDigitalMiningProduct()
    {
        if (MetallicStuffs.Empty())
        {
            MetallicStuffs = DefDatabase<ThingDef>.AllDefs.Where(def =>
                def.stuffProps != null && def.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic)).ToList();
        }

        const int targetValue = 1000;
        var currentValue = 0f;
        var products = new ThingOwner<Thing>();
        while (currentValue < targetValue)
        {
            var thing = ThingMaker.MakeThing(MetallicStuffs.RandomElement());
            products.TryAdd(thing, 1);
            currentValue += thing.GetStatValue(StatDefOf.MarketValue);
        }

        for (var i = products.Count - 1; i >= 0; i--)
        {
            var item = products[i];
            products.Remove(item);
            yield return item;
        }
    }
}