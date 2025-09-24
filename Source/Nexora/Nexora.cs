using HarmonyLib;
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
            Nexora.harmony.PatchCategory("VEF_PipeSystem");
        }
    }
}