using HarmonyLib;
using Verse;

namespace Nexora;

public class Nexora : Mod
{
    public Nexora(ModContentPack content) : base(content)
    {
        new Harmony("dev.soeur.nexora").PatchAll();
    }
}