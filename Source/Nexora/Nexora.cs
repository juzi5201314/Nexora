using HarmonyLib;
using Nexora.VEF_PipeSystem;
using Verse;

namespace Nexora;

public class Nexora : Mod
{
    internal static Harmony harmony = new("dev.soeur.nexora");

    public Nexora(ModContentPack content) : base(content)
    {
        harmony.PatchAllUncategorized();
    }
}

[StaticConstructorOnStartup]
public static class OnStartup
{
    static OnStartup()
    {
        if (LoadedModManager.RunningMods.Any(m => m.PackageId == "oskarpotocki.vanillafactionsexpanded.core"))
        {
            Nexora.harmony.Patch(
                AccessTools.PropertyGetter(AccessTools.TypeByName("PipeSystem.CompResourceStorage"), "AmountCanAccept"),
                AccessTools.Method(typeof(CompResourceStoragePatch), "AmountCanAccept")
            );
            Nexora.harmony.Patch(
                AccessTools.PropertyGetter(AccessTools.TypeByName("PipeSystem.CompResourceStorage"), "AmountStored"),
                AccessTools.Method(typeof(CompResourceStoragePatch), "AmountStored")
            );
            Nexora.harmony.Patch(
                AccessTools.PropertyGetter(AccessTools.TypeByName("PipeSystem.CompResourceStorage"), "AmountStoredPct"),
                AccessTools.Method(typeof(CompResourceStoragePatch), "AmountStoredPct")
            );
            Nexora.harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("PipeSystem.CompResourceStorage"), "AddResource"),
                AccessTools.Method(typeof(CompResourceStoragePatch), "AddResource")
            );
            Nexora.harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("PipeSystem.CompResourceStorage"), "DrawResource"),
                AccessTools.Method(typeof(CompResourceStoragePatch), "DrawResource")
            );
            Nexora.harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("PipeSystem.PipeNetMaker"), "MakePipeNet"),
                null, AccessTools.Method(typeof(PipeNetMakerPatch), "MakePipeNet")
            );
        }

        if (LoadedModManager.RunningMods.Any(m => m.PackageId == "vanillaexpanded.vnutriente"))
        {
            Nexora.harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("VNPE.Building_NutrientGrinder"), "FindFeedInAnyHopper")
                , null, AccessTools.Method(typeof(VNPE_Patches), "FindFeedInAnyHopper")
            );
            Nexora.harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("VNPE.Building_NutrientGrinder"), "HasEnoughFeed")
                , AccessTools.Method(typeof(VNPE_Patches), "HasEnoughFeed")
            );
        }
    }
}